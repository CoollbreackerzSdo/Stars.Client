using System;
using System.Collections.Generic;

#if ANDROID
using Android.Content;
using Android.Media;
#elif WINDOWS
using NAudio.Wave;
#endif

namespace Stars.Client.Features.Platforms;

/// <summary>A selectable audio output. <see cref="Id"/> is "default" or a platform device id.</summary>
public sealed record AudioOutputDevice(string Id, string Name, bool IsDefault)
{
    public const string DefaultId = "default";

    public static AudioOutputDevice Default { get; } = new(DefaultId, "Predeterminado del sistema", true);

    public override string ToString() => Name;
}

/// <summary>Enumerates the audio outputs available on the current platform.</summary>
public static class AudioOutputs
{
    public static IReadOnlyList<AudioOutputDevice> List()
    {
        var devices = new List<AudioOutputDevice> { AudioOutputDevice.Default };

        try
        {
#if WINDOWS
            var count = WaveOut.DeviceCount;
            for (var i = 0; i < count; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                devices.Add(new AudioOutputDevice(i.ToString(), caps.ProductName, false));
            }
#elif ANDROID
            var manager = (AudioManager?)Android.App.Application.Context.GetSystemService(Context.AudioService);
            var found = manager?.GetDevices(GetDevicesTargets.Outputs);
            if (found is not null)
            {
                foreach (var device in found)
                {
                    var name = string.IsNullOrWhiteSpace(device.ProductName)
                        ? $"Salida {device.Id}"
                        : $"{device.ProductName} ({device.Type})";
                    devices.Add(new AudioOutputDevice(device.Id.ToString(), name, false));
                }
            }
#endif
        }
        catch
        {
            // Enumeration failed; the default entry is enough to keep playback working.
        }

        return devices;
    }
}
