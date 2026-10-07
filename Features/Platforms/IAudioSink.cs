using System;

namespace Stars.Client.Features.Platforms;

/// <summary>
/// Platform abstraction over live PCM playback. Implementations accept raw 16-bit
/// little-endian interleaved samples.
/// </summary>
public interface IAudioSink : IDisposable
{
    void Start();

    /// <summary>Writes PCM bytes to the output device. Should never block for long.</summary>
    void Write(byte[] pcm, int offset, int count);

    void Stop();
}

/// <summary>Creates the audio sink implementation for the current operating system.</summary>
public static class AudioSinkFactory
{
    public static IAudioSink Create(int sampleRate, int channels, AudioOutputDevice? device = null)
    {
#if WINDOWS
        return new WindowsAudioSink(sampleRate, channels, device);
#elif ANDROID
        return new AndroidAudioSink(sampleRate, channels, device);
#else
        return new NullAudioSink();
#endif
    }
}
