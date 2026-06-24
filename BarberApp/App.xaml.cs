using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;using BarberApp.Views;

namespace BarberApp;

public partial class App : Application
{
    private readonly DatabaseService _databaseService;
    private readonly LicenciaService _licenciaService;
    private readonly BackupService _backupService;
    private readonly NotificationService _notificationService;

    public App(
        DatabaseService databaseService,
        LicenciaService licenciaService,
        BackupService backupService,
        NotificationService notificationService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _licenciaService = licenciaService;
        _backupService = backupService;
        _notificationService = notificationService;
        ThemeService.AplicarTemaGuardado();
        ThemeService.ActualizarRecursos();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = _notificationService.ProgramarRecordatoriosAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(new ContentPage
        {
            BackgroundColor = ThemeService.GetColor("PageBackground"),
            Content = new ActivityIndicator
            {
                IsRunning = true,
                Color = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        }));

        window.Created += async (_, _) => await ConfigurarNavegacionInicialAsync();
        return window;
    }

    private async Task ConfigurarNavegacionInicialAsync()
    {
        await _databaseService.InitAsync();
        await _licenciaService.RestaurarLicenciaAlInicioAsync();

        Page paginaInicial;

        if (!Preferences.Get(PreferenceKeys.LicenciaActivada, false))
        {
            paginaInicial = new LicenciaPage(_licenciaService, _databaseService, _backupService);
        }
        else if (LicenciaHelper.EstaVencida())
        {
            paginaInicial = new PantallaBloqueoPage(_licenciaService, _databaseService, _backupService);
        }
        else if (await _databaseService.GetBarberosCountAsync() == 0)
        {
            paginaInicial = new RegistroAdminPage(_databaseService, _licenciaService, _backupService);
        }
        else
        {
            paginaInicial = new LoginPage(_databaseService, _licenciaService, _backupService);
        }

        NavigationHelper.SetRootPage(paginaInicial);
    }
}
