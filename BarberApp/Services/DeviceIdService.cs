using BarberApp.Core;
using BarberApp.Models;

namespace BarberApp.Services;

public class DeviceIdService
{
    public string ObtenerDeviceId()
    {
        var installId = Preferences.Get(PreferenceKeys.DeviceInstallId, "");
        if (string.IsNullOrEmpty(installId))
        {
            installId = Guid.NewGuid().ToString("N");
            Preferences.Set(PreferenceKeys.DeviceInstallId, installId);
        }

        return DeviceIdHelper.GenerarId(ObtenerSemillaPlataforma(), installId);
    }

    public string ObtenerIdInstalacion() =>
        Preferences.Get(PreferenceKeys.DeviceInstallId, "");

    private static string ObtenerSemillaPlataforma()
    {
#if WINDOWS
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography");
            var machineGuid = key?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrWhiteSpace(machineGuid))
                return machineGuid;
        }
        catch { /* sandbox o permisos */ }
        return Environment.MachineName;
#elif ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var androidId = Android.Provider.Settings.Secure.GetString(
                context.ContentResolver,
                Android.Provider.Settings.Secure.AndroidId);
            if (!string.IsNullOrWhiteSpace(androidId))
                return androidId;
        }
        catch { /* ignore */ }
        return DeviceInfo.Current.Model;
#elif IOS || MACCATALYST
        return UIKit.UIDevice.CurrentDevice.IdentifierForVendor?.AsString() ?? DeviceInfo.Current.Model;
#else
        return DeviceInfo.Current.Model + DeviceInfo.Current.Platform;
#endif
    }
}
