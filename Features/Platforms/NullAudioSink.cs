namespace Stars.Client.Features.Platforms;

/// <summary>No-op sink for platforms without an implementation yet.</summary>
public sealed class NullAudioSink : IAudioSink
{
    public void Start()
    {
    }

    public void Write(byte[] pcm, int offset, int count)
    {
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }
}
