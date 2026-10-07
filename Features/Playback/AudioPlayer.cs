using System.Diagnostics;
using System.Threading;
using Concentus;
using Stars.Client.Features.Platforms;

namespace Stars.Client.Features.Playback;

/// <summary>
/// Decodes incoming Opus packets and plays them through a steady, jitter-buffered pipeline.
///
/// Network reception and playback are decoupled: received packets go into a
/// <see cref="JitterBuffer"/>, and a dedicated playback loop releases exactly one frame every
/// 20 ms to the audio sink. A single long-running loop (never overlapping timer callbacks) plus
/// full exception shielding keeps playback stable — and, crucially, prevents an exception on the
/// audio thread from tearing down the process.
/// </summary>
public sealed class AudioPlayer : IDisposable
{
    private readonly Lock _gate = new();
    private readonly JitterBuffer _jitter = new();

    private IOpusDecoder? _decoder;
    private IAudioSink? _sink;

    private CancellationTokenSource? _playCts;
    private Task? _playLoop;

    private int _sampleRate;
    private int _channels;
    private int _outputChannels;
    private int _frameSamples;

    private short[] _pcm = [];
    private byte[] _pcmBytes = [];
    private byte[] _silenceBytes = [];

    private double _volume = PhoneAudioVolume.GetMediaVolume();
    private AudioOutputDevice _output = AudioOutputDevice.Default;
    private bool _primed;

    public bool IsRunning { get; private set; }

    public AudioOutputDevice Output => _output;

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 1);
            PhoneAudioVolume.SetMediaVolume(_volume);
        }
    }

    public int BufferMilliseconds => _jitter.BufferMilliseconds;

    public double JitterMilliseconds => _jitter.JitterMs;

    public long Underruns => _jitter.Underruns;

    public long Received => _jitter.Received;

    public event Action<string>? Log;

    public void Start(int sampleRate, int channels, int frameSamplesPerChannel, AudioOutputDevice? device = null)
    {
        // Tear down any previous session first (handles its own locking).
        Stop();

        lock (_gate)
        {
            _output = device ?? _output;
            _sampleRate = sampleRate;
            _channels = channels;
            _outputChannels = channels == 1 ? 2 : channels;
            _frameSamples = frameSamplesPerChannel;

            _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);
            _sink = CreateSink(sampleRate, _outputChannels, _output);
            if (_sink is null)
            {
                _decoder.Dispose();
                _decoder = null;
                Log?.Invoke("No se pudo abrir el dispositivo de audio.");
                return;
            }

            var samples = frameSamplesPerChannel * channels;
            _pcm = new short[samples];
            var outputSamples = frameSamplesPerChannel * _outputChannels;
            _pcmBytes = new byte[outputSamples * sizeof(short)];
            _silenceBytes = new byte[outputSamples * sizeof(short)];

            _jitter.Reset();
            _primed = false;

            IsRunning = true;
            _playCts = new CancellationTokenSource();
            _playLoop = Task.Run(() => PlaybackLoopAsync(_playCts.Token));

            Log?.Invoke($"Reproducción iniciada en «{_output.Name}» ({sampleRate} Hz · {channels} ch).");
        }
    }

    private IAudioSink? CreateSink(int sampleRate, int channels, AudioOutputDevice output)
    {
        try
        {
            var sink = AudioSinkFactory.Create(sampleRate, channels, output);
            sink.Start();
            return sink;
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Error al abrir el audio: {ex.Message}");
            return null;
        }
    }

    /// <summary>Switches the output device while playback keeps running.</summary>
    public void SetOutput(AudioOutputDevice device)
    {
        lock (_gate)
        {
            if (_output == device)
                return;

            _output = device;

            if (!IsRunning)
                return;

            // Stop the sink and build the new one. The playback loop tolerates a null sink
            // during the swap and resumes as soon as the new one is ready.
            try
            {
                _sink?.Stop();
                _sink?.Dispose();
            }
            catch
            {
                // ignore
            }

            _sink = CreateSink(_sampleRate, _outputChannels, _output);
            Log?.Invoke($"Salida de audio: «{_output.Name}».");
        }
    }

    /// <summary>Called from the receive loop: queues one encoded packet.</summary>
    public void Enqueue(byte[] data, int offset, int length)
    {
        lock (_gate)
        {
            if (!IsRunning)
                return;

            var packet = new byte[length];
            Buffer.BlockCopy(data, offset, packet, 0, length);
            _jitter.Push(packet);
        }
    }

    /// <summary>
    /// Single, non-overlapping playback loop. The pacing comes from the sink's blocking write
    /// (the audio device clock), not from a timer — so there are no scheduling gaps. Runs on its
    /// own task and swallows every exception so audio issues can never crash the app.
    /// </summary>
    private async Task PlaybackLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var ready = PlayOneFrame();
                if (!ready)
                {
                    // Still pre-rolling: wait briefly for the buffer to fill before we start.
                    await Task.Delay(5, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Error de reproducción: {ex.Message}");
            }
        }
    }

    /// <summary>Pumps one frame. Returns false while waiting for the pre-roll buffer to fill.</summary>
    private bool PlayOneFrame()
    {
        IAudioSink? sink;
        IOpusDecoder? decoder;
    #if !ANDROID
        double volume;
    #endif
        byte[]? packet = null;

        lock (_gate)
        {
            if (!IsRunning || _sink is null)
                return false;

            sink = _sink;
            decoder = _decoder;
#if !ANDROID
            volume = _volume;
#endif

            // Pre-roll: wait until enough frames are buffered to absorb network jitter.
            if (!_primed)
            {
                if (_jitter.Available >= _jitter.TargetDepth)
                    _primed = true;
                else
                    return false;
            }

            _jitter.TryDequeue(out packet);
        }

        if (sink is null)
            return false;

        if (packet is null || decoder is null)
        {
            // Underrun: keep the device fed with silence (blocking, paced) so it never starves.
            WriteSafely(sink, _silenceBytes, _silenceBytes.Length);
            return true;
        }

        var decoded = decoder.Decode(packet, _pcm, _frameSamples);
        if (decoded <= 0)
            return true;

        var sampleCount = decoded * _channels;
#if !ANDROID
        if (volume < 0.999)
            ApplyGain(_pcm, sampleCount, volume);
#endif

        int outputSampleCount;
        if (_channels == 1 && _outputChannels == 2)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var sample = _pcm[i];
                var outputIndex = i * 2;
                _pcmBytes[outputIndex * sizeof(short)] = (byte)(sample & 0xFF);
                _pcmBytes[outputIndex * sizeof(short) + 1] = (byte)(sample >> 8);
                _pcmBytes[outputIndex * sizeof(short) + 2] = (byte)(sample & 0xFF);
                _pcmBytes[outputIndex * sizeof(short) + 3] = (byte)(sample >> 8);
            }

            outputSampleCount = sampleCount * 2;
        }
        else
        {
            outputSampleCount = sampleCount;
            Buffer.BlockCopy(_pcm, 0, _pcmBytes, 0, outputSampleCount * sizeof(short));
        }

        var byteCount = outputSampleCount * sizeof(short);
        WriteSafely(sink, _pcmBytes, byteCount);
        return true;
    }

    private void WriteSafely(IAudioSink sink, byte[] buffer, int count)
    {
        try
        {
            sink.Write(buffer, 0, count);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Error al escribir audio: {ex.Message}");
        }
    }

    private static void ApplyGain(short[] pcm, int count, double volume)
    {
        for (var i = 0; i < count; i++)
            pcm[i] = (short)(pcm[i] * volume);
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Task? loop;

        // Mark stopped and detach the loop without holding the lock while waiting, so the
        // playback loop (which also takes the lock) can observe cancellation and exit.
        lock (_gate)
        {
            IsRunning = false;
            cts = _playCts;
            loop = _playLoop;
            _playCts = null;
            _playLoop = null;
        }

        cts?.Cancel();
        try
        {
            loop?.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch
        {
            // ignore
        }

        cts?.Dispose();

        IAudioSink? sink;
        IOpusDecoder? decoder;
        lock (_gate)
        {
            sink = _sink;
            decoder = _decoder;
            _sink = null;
            _decoder = null;
            _primed = false;
        }

        try
        {
            sink?.Stop();
            sink?.Dispose();
        }
        catch
        {
            // ignore
        }

        decoder?.Dispose();
    }

    public void Dispose() => Stop();
}
