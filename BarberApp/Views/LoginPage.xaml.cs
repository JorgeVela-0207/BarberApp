using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;

namespace BarberApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly LicenciaService _licenciaService;
    private readonly BackupService _backupService;

    public LoginPage(
        DatabaseService databaseService,
        LicenciaService licenciaService,
        BackupService backupService)
    {
        InitializeComponent();
#if WINDOWS
        MauiProgram.ResizarVentanaActual(460, 580);
#endif
        _databaseService = databaseService;
        _licenciaService = licenciaService;
        _backupService = backupService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.AplicarAPagina(this);
        LblNombreLocal.Text = Preferences.Get(PreferenceKeys.NombreLocal, "Mi negocio");
        LblError.IsVisible = false;

        if (LicenciaHelper.DebeMostrarAviso() && LicenciaHelper.DiasRestantes() is int dias)
        {
            LblAvisoLicencia.IsVisible = true;
            LblAvisoLicencia.Text = $"⚠️ Licencia vence en {dias} día(s)";
        }
        else
            LblAvisoLicencia.IsVisible = false;
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        LblError.IsVisible = false;

        if (LicenciaHelper.EstaVencida())
        {
            NavigationHelper.SetRootPage(new PantallaBloqueoPage(_licenciaService, _databaseService, _backupService));
            return;
        }

        var usuario = EntryUsuario.Text?.Trim() ?? string.Empty;
        var password = EntryPassword.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
        {
            LblError.IsVisible = true;
            return;
        }

        var barbero = await _databaseService.LoginAsync(usuario, password);

        if (barbero == null)
        {
            LblError.IsVisible = true;
            return;
        }

        SessionService.IniciarSesion(barbero);

        NavigationHelper.SetRootPage(PageFactory.Dashboard());
    }
}
