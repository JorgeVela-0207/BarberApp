using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;

namespace BarberApp.Views;

public partial class PantallaBloqueoPage : ContentPage
{
    private readonly LicenciaService _licenciaService;
    private readonly DatabaseService _databaseService;
    private readonly BackupService _backupService;
    private string _deviceId = string.Empty;

    public PantallaBloqueoPage(
        LicenciaService licenciaService,
        DatabaseService databaseService,
        BackupService backupService)
    {
        InitializeComponent();
        _licenciaService = licenciaService;
        _databaseService = databaseService;
        _backupService = backupService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.AplicarAPagina(this);
        _deviceId = _licenciaService.ObtenerDeviceId();
        LblDeviceId.Text = $"Device ID: {_deviceId}";
    }

    private async void OnCopiarIdClicked(object? sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(_deviceId);
        await DisplayAlert("Copiado", "Device ID copiado al portapapeles", "OK");
    }

    private async void OnVerificarPagoClicked(object? sender, EventArgs e)
    {
        IndicatorCarga.IsVisible = true;
        IndicatorCarga.IsRunning = true;

        try
        {
            var deviceId = _licenciaService.ObtenerDeviceId();
            var licencia = await _licenciaService.VerificarRenovacionAsync(deviceId);

            if (licencia == null)
            {
                var hayInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
                if (!hayInternet)
                    await DisplayAlert("Sin conexión", "Se requiere internet para verificar", "OK");
                else
                    await DisplayAlert("Licencia",
                        "Aún no hay renovación activa para este equipo. Envía tu Device ID a soporte si cambiaste de PC.",
                        "OK");
                return;
            }

            var token = Preferences.Get(PreferenceKeys.LicenciaToken, licencia.Token);
            LicenciaService.GuardarPreferenciasLicencia(licencia, token, _licenciaService.ObtenerDeviceId());
            NavigationHelper.SetRootPage(new LoginPage(_databaseService, _licenciaService, _backupService));
        }
        finally
        {
            IndicatorCarga.IsRunning = false;
            IndicatorCarga.IsVisible = false;
        }
    }
}
