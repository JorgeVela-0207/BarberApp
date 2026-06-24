using BarberApp.Models;

namespace BarberApp.Services;

public class NotificationService
{
    private readonly DatabaseService _db;
    private bool _permisoSolicitado;

    public NotificationService(DatabaseService databaseService)
    {
        _db = databaseService;
    }

    public Task AsegurarPermisosAsync()
    {
        if (_permisoSolicitado)
            return Task.CompletedTask;

        _permisoSolicitado = true;

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return SolicitarPermisoNotificacionesAndroidAsync();
#endif
        return Task.CompletedTask;
    }

#if ANDROID
    private static async Task SolicitarPermisoNotificacionesAndroidAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.PostNotifications>();
    }
#endif

    public async Task ProgramarRecordatoriosAsync()
    {
        await AsegurarPermisosAsync();

        var citas = await _db.GetCitasParaNotificarAsync();
        foreach (var cita in citas)
        {
            await MostrarNotificacionAsync(
                "Cita en 1 hora",
                $"{cita.NombreCliente} · {cita.NombreServicio} a las {cita.Hora}");
            await _db.MarcarNotificacionEnviadaAsync(cita.Id);
        }
    }

    public Task MostrarNotificacionAsync(string titulo, string mensaje)
    {
#if ANDROID
        return MostrarAndroidAsync(titulo, mensaje);
#elif WINDOWS
        return MostrarWindowsAsync(titulo, mensaje);
#else
        return Task.CompletedTask;
#endif
    }

#if ANDROID
    private Task MostrarAndroidAsync(string titulo, string mensaje)
    {
        try
        {
            var context = Android.App.Application.Context;
            var channelId = "barberapp_citas";
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var channel = new Android.App.NotificationChannel(
                    channelId, "Citas", Android.App.NotificationImportance.Default);
                var nm = context.GetSystemService(Android.Content.Context.NotificationService)
                    as Android.App.NotificationManager;
                nm?.CreateNotificationChannel(channel);
            }

            var builder = new AndroidX.Core.App.NotificationCompat.Builder(context, channelId)
                .SetContentTitle(titulo)
                .SetContentText(mensaje)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true);

            var nm2 = AndroidX.Core.App.NotificationManagerCompat.From(context);
            nm2.Notify(Random.Shared.Next(), builder.Build());
        }
        catch { /* permisos no concedidos */ }
        return Task.CompletedTask;
    }
#endif

#if WINDOWS
    private Task MostrarWindowsAsync(string titulo, string mensaje)
    {
        try
        {
            var builder = new Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder()
                .AddText(titulo)
                .AddText(mensaje);
            Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Show(builder.BuildNotification());
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page != null)
                    await page.DisplayAlert(titulo, mensaje, "OK");
            });
        }
        return Task.CompletedTask;
    }
#endif
}
