#if ANDROID
using Android.Content;
using Android.Media;

namespace Stars.Client.Features.Platforms;

/// <summary>
/// Android playback sink using AudioTrack in streaming mode with a small buffer
/// tuned for low-latency playback. Optionally prefers a specific output device.
/// </summary>
public sealed class AndroidAudioSink : IAudioSink
{
    private const int PreferredBufferDurationMs = 40;

    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly int _preferredDeviceId;
    private AudioTrack? _track;

    public AndroidAudioSink(int sampleRate, int channels, AudioOutputDevice? device = null)
    {
        _sampleRate = sampleRate;
        _channels = channels;
        _preferredDeviceId = device is { IsDefault: false } && int.TryParse(device.Id, out var id) ? id : 0;
    }

    public void Start()
    {
        if (_track is not null)
            return;

        var channelMask = _channels == 1 ? ChannelOut.Mono : ChannelOut.Stereo;
        var minBuffer = AudioTrack.GetMinBufferSize(_sampleRate, channelMask, Encoding.Pcm16bit);
        // Small buffer keeps latency low; never go below what the device requires.
        var targetBytes = _sampleRate * _channels * sizeof(short) * PreferredBufferDurationMs / 1000;
        var bufferSize = System.Math.Max(minBuffer, targetBytes);

        var attributes = new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Media)!
            .SetContentType(AudioContentType.Music)!
            .Build()!;

        var format = new AudioFormat.Builder()
            .SetEncoding(Encoding.Pcm16bit)!
            .SetSampleRate(_sampleRate)!
            .SetChannelMask(channelMask)!
            .Build()!;

        var builder = new AudioTrack.Builder()
            .SetAudioAttributes(attributes)!
            .SetAudioFormat(format)!
            .SetBufferSizeInBytes(bufferSize)!
            .SetTransferMode(AudioTrackMode.Stream)!;

        _track = builder.Build();

        // Pin playback to a specific output when the user picked one.
        if (_preferredDeviceId != 0)
        {
            var preferred = ResolveDevice(_preferredDeviceId);
            if (preferred is not null)
                _track.SetPreferredDevice(preferred);
        }

        _track.Play();
    }

    private static AudioDeviceInfo? ResolveDevice(int id)
    {
        try
        {
            var manager = (AudioManager?)Android.App.Application.Context.GetSystemService(Context.AudioService);
            var devices = manager?.GetDevices(GetDevicesTargets.Outputs);
            if (devices is null)
                return null;

            foreach (var device in devices)
            {
                if (device.Id == id)
                    return device;
            }
        }
        catch
        {
            // fall back to the default routing
        }

        return null;
    }

    public void Write(byte[] pcm, int offset, int count)
    {
        if (_track is null || count <= 0)
            return;

        // Blocking write applies natural backpressure so the socket read never overruns playback.
        _track.Write(pcm, offset, count, WriteMode.Blocking);
    }

    public void Stop()
    {
        try
        {
            _track?.Pause();
            _track?.Flush();
            _track?.Stop();
            _track?.Release();
            _track?.Dispose();
        }
        catch
        {
            // ignore
        }

        _track = null;
    }

    public void Dispose() => Stop();
}
#endif
