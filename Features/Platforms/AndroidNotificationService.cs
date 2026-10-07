#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;

namespace Stars.Client.Features.Platforms;

/// <summary>
/// Android implementation using a persistent notification on a low-importance channel.
/// The notification reflects that Stars is streaming audio back from the PC.
/// </summary>
public sealed class AndroidNotificationService : INotificationService
{
    private const int NotificationId = 4201;
    private const string ChannelId = "stars_playback";

    public void ShowPlaying(string text)
    {
        try
        {
            var context = Android.App.Application.Context;
            EnsureChannel(context);

            Notification.Builder builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? new Notification.Builder(context, ChannelId)
                : new Notification.Builder(context);

            builder
                .SetContentTitle("Stars")
                .SetContentText(text)
                .SetSmallIcon(Android.Resource.Drawable.IcMediaPlay)
                .SetOngoing(true)
                .SetOnlyAlertOnce(true);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                builder.SetChannelId(ChannelId);

            var launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
            if (launch is not null)
            {
                launch.AddFlags(ActivityFlags.SingleTop);
                var pending = PendingIntent.GetActivity(
                    context, 0, launch, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
                if (pending is not null)
                    builder.SetContentIntent(pending);
            }

            var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            manager?.Notify(NotificationId, builder.Build());
        }
        catch
        {
            // A failed notification must never affect playback.
        }
    }

    public void Clear()
    {
        try
        {
            var manager = (NotificationManager?)Android.App.Application.Context
                .GetSystemService(Context.NotificationService);
            manager?.Cancel(NotificationId);
        }
        catch
        {
            // ignore
        }
    }

    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null || manager.GetNotificationChannel(ChannelId) is not null)
            return;

        var channel = new NotificationChannel(ChannelId, "Reproducción", NotificationImportance.Low)
        {
            Description = "Indica cuándo Stars está reproduciendo audio."
        };

        manager.CreateNotificationChannel(channel);
    }
}
#endif
