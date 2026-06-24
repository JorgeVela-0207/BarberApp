using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;

namespace BarberApp.Views;

public partial class RegistroAdminPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly LicenciaService _licenciaService;
    private readonly BackupService _backupService;

    public RegistroAdminPage(
        DatabaseService databaseService,
        LicenciaService licenciaService,
        BackupService backupService)
    {
        InitializeComponent();
#if WINDOWS
        MauiProgram.ResizarVentanaActual(460, 740);
#endif
        _databaseService = databaseService;
        _licenciaService = licenciaService;
        _backupService = backupService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.AplicarAPagina(this);
        LblNombreLocal.Text = "Configura los datos de tu negocio";
    }

    private async void OnCrearCuentaClicked(object? sender, EventArgs e)
    {
        var nombreNegocio = EntryNombreNegocio.Text?.Trim() ?? string.Empty;
        var nombre = EntryNombre.Text?.Trim() ?? string.Empty;
        var usuario = EntryUsuario.Text?.Trim() ?? string.Empty;
        var password = EntryPassword.Text ?? string.Empty;
        var confirmar = EntryConfirmar.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombreNegocio) ||
            string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmar))
        {
            await DisplayAlert("Error", "Todos los campos son obligatorios", "OK");
            return;
        }

        if (usuario.Contains(' '))
        {
            await DisplayAlert("Error", "El usuario no puede contener espacios", "OK");
            return;
        }

        if (password != confirmar)
        {
            await DisplayAlert("Error", "Las contraseñas no coinciden", "OK");
            return;
        }

        if (await _databaseService.UsuarioExisteAsync(usuario))
        {
            await DisplayAlert("Error", "Ese usuario ya existe", "OK");
            return;
        }

        Preferences.Set(PreferenceKeys.NombreLocal, nombreNegocio);

        var barbero = new Barbero
        {
            Nombre = nombre,
            Usuario = usuario,
            PasswordHash = PasswordHelper.Hash(password),
            Rol = Roles.Admin,
            Activo = true
        };

        await _databaseService.SaveBarberoAsync(barbero);

        NavigationHelper.SetRootPage(new LoginPage(_databaseService, _licenciaService, _backupService));
    }
}
