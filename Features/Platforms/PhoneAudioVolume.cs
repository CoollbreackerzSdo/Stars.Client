using System;

#if ANDROID
using Android.Content;
using Android.Media;
using MediaStream = Android.Media.Stream;
#endif

namespace Stars.Client.Features.Platforms;

internal static class PhoneAudioVolume
{
    public static double GetMediaVolume()
    {
#if ANDROID
        try
        {
            var manager = GetAudioManager();
            var maximum = manager?.GetStreamMaxVolume(MediaStream.Music) ?? 0;
            if (maximum > 0)
                return (double)manager!.GetStreamVolume(MediaStream.Music) / maximum;
        }
        catch
        {
            // Keep the client usable if Android does not expose the media volume.
        }
#endif
        return 0.8;
    }

    public static void SetMediaVolume(double volume)
    {
#if ANDROID
        try
        {
            var manager = GetAudioManager();
            if (manager is null)
                return;

            var maximum = manager.GetStreamMaxVolume(MediaStream.Music);
            if (maximum <= 0)
                return;

            var level = (int)Math.Round(Math.Clamp(volume, 0, 1) * maximum, MidpointRounding.AwayFromZero);
            manager.SetStreamVolume(MediaStream.Music, level, VolumeNotificationFlags.RemoveSoundAndVibrate);
        }
        catch
        {
            // A volume-control failure must not interrupt playback.
        }
#endif
    }

#if ANDROID
    private static AudioManager? GetAudioManager() =>
        (AudioManager?)Android.App.Application.Context.GetSystemService(Context.AudioService);
#endif
}