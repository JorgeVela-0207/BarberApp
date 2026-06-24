using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using BarberApp.Models;
using BarberApp.Services;
using BarberApp.Views;

namespace BarberApp
{
    public static class MauiProgram
    {
#if WINDOWS
        public static void ResizarVentana(Microsoft.UI.Xaml.Window window, int ancho, int alto)
        {
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(ancho, alto));
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                window.AppWindow.Id,
                Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            var x = (displayArea.WorkArea.Width - ancho) / 2;
            var y = (displayArea.WorkArea.Height - alto) / 2;
            window.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }

        public static void ResizarVentanaActual(int ancho, int alto)
        {
            if (Application.Current?.Windows[0].Handler?.PlatformView
                is Microsoft.UI.Xaml.Window win)
            {
                ResizarVentana(win, ancho, alto);
            }
        }
#endif
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(w =>
                {
                    w.OnWindowCreated(window =>
                    {
                        window.Activate();
                        ResizarVentana(window, 460, 580);
                    });
                    w.OnLaunched((_, _) =>
                    {
                        try
                        {
                            Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Register();
                        }
                        catch { /* entorno sin empaquetado */ }
                    });
                });
            });
#endif

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<DeviceIdService>();
            builder.Services.AddSingleton<LicenciaService>();
            builder.Services.AddSingleton<BackupService>();
            builder.Services.AddSingleton<NotificationService>();
            builder.Services.AddSingleton<ReportService>();
            builder.Services.AddSingleton<SyncService>();

            builder.Services.AddTransient<LicenciaPage>();
            builder.Services.AddTransient<RegistroAdminPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<PantallaBloqueoPage>();
            builder.Services.AddTransient<CatalogosPage>();
            builder.Services.AddTransient<AgendaPage>();
            builder.Services.AddTransient<CajaPage>();
            builder.Services.AddTransient<ConfiguracionPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            var licenciaToken = Environment.GetEnvironmentVariable("BARBERAPP_LICENCIA_TOKEN");
            if (!string.IsNullOrWhiteSpace(licenciaToken))
                LicenciaConfig.ConfigurarApiToken(licenciaToken);

            return app;
        }
    }
}
