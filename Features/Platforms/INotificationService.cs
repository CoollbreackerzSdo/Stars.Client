namespace Stars.Client.Features.Platforms;

/// <summary>
/// Shows the "playing audio" notification while the client streams, and clears it on stop.
/// </summary>
public interface INotificationService
{
    void ShowPlaying(string text);

    void Clear();
}

/// <summary>Creates the notification implementation for the current platform.</summary>
public static class NotificationServiceFactory
{
    public static INotificationService Create()
    {
#if ANDROID
        return new AndroidNotificationService();
#else
        return new NullNotificationService();
#endif
    }
}

/// <summary>No-op notification service for platforms without an implementation.</summary>
public sealed class NullNotificationService : INotificationService
{
    public void ShowPlaying(string text)
    {
    }

    public void Clear()
    {
    }
}
