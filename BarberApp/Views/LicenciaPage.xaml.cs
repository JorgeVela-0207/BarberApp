using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;

namespace BarberApp.Views;

public partial class LicenciaPage : ContentPage
{
    private readonly LicenciaService _licenciaService;
    private readonly DatabaseService _databaseService;
    private readonly BackupService _backupService;
    private string _deviceId = string.Empty;

    public LicenciaPage(
        LicenciaService licenciaService,
        DatabaseService databaseService,
        BackupService backupService)
    {
        InitializeComponent();
#if WINDOWS
        MauiProgram.ResizarVentanaActual(460, 680);
#endif
        _licenciaService = licenciaService;
        _databaseService = databaseService;
        _backupService = backupService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.AplicarAPagina(this);
        _deviceId = _licenciaService.ObtenerDeviceId();
        LblDeviceId.Text = _deviceId;

        var tokenGuardado = Preferences.Get(PreferenceKeys.LicenciaToken, string.Empty);
        if (string.IsNullOrWhiteSpace(EntryToken.Text) && !string.IsNullOrWhiteSpace(tokenGuardado))
            EntryToken.Text = tokenGuardado;
    }

    private async void OnCopiarIdClicked(object? sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(_deviceId);
        await DisplayAlert("Copiado", "Device ID copiado al portapapeles", "OK");
    }

    private async void OnActivarClicked(object? sender, EventArgs e)
    {
        var token = EntryToken.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            await DisplayAlert("Error", "Ingresa un token de licencia", "OK");
            return;
        }

        IndicatorCarga.IsVisible = true;
        IndicatorCarga.IsRunning = true;

        try
        {
            var (resultado, licencia) = await _licenciaService.ActivarAsync(token);

            if (resultado != ResultadoActivacionLicencia.Exito || licencia == null)
            {
                await DisplayAlert("Activación", LicenciaService.MensajeActivacion(resultado), "OK");
                return;
            }

            _licenciaService.PersistirLicencia(licencia, token);

            if (await _databaseService.GetBarberosCountAsync() == 0)
                NavigationHelper.SetRootPage(new RegistroAdminPage(_databaseService, _licenciaService, _backupService));
            else
                NavigationHelper.SetRootPage(new LoginPage(_databaseService, _licenciaService, _backupService));
        }
        finally
        {
            IndicatorCarga.IsRunning = false;
            IndicatorCarga.IsVisible = false;
        }
    }
}
