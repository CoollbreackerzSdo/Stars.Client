#if WINDOWS
using NAudio.Wave;

namespace Stars.Client.Features.Platforms;

/// <summary>Windows playback sink backed by a buffered WaveOut device.</summary>
public sealed class WindowsAudioSink : IAudioSink
{
    private readonly WaveFormat _format;
    private readonly int _deviceNumber;
    private BufferedWaveProvider? _buffer;
    private WaveOut? _output;

    public WindowsAudioSink(int sampleRate, int channels, AudioOutputDevice? device = null)
    {
        _format = new WaveFormat(sampleRate, 16, channels);
        _deviceNumber = device is { IsDefault: false } && int.TryParse(device.Id, out var n) ? n : -1;
    }

    public void Start()
    {
        if (_output is not null)
            return;

        // Small buffer (with drop-oldest) keeps latency low and bounded.
        _buffer = new BufferedWaveProvider(_format, TimeSpan.FromMilliseconds(250))
        {
            DiscardOnBufferOverflow = true
        };

        // Shorter shared-mode buffer than the default (~300 ms).
        _output = new WaveOut { BufferMilliseconds = 60, NumberOfBuffers = 3 };
        if (_deviceNumber >= 0)
            _output.DeviceNumber = _deviceNumber;

        _output.Init(_buffer);
        _output.Play();
    }

    public void Write(byte[] pcm, int offset, int count)
    {
        if (_buffer is null || count <= 0)
            return;

        _buffer.AddSamples(pcm.AsSpan(offset, count));
    }

    public void Stop()
    {
        try
        {
            _output?.Stop();
            _output?.Dispose();
        }
        catch
        {
            // ignore
        }

        _output = null;
        _buffer = null;
    }

    public void Dispose() => Stop();
}
#endif
