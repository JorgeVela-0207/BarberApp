using BarberApp.Core;
using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;
using System.Collections.ObjectModel;

namespace BarberApp.Views;

public partial class ConfiguracionPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly BackupService _backup;
    private readonly LicenciaService _licencia;
    private readonly SyncService _sync;

    public ObservableCollection<AuditoriaItem> Auditoria { get; } = new();

    public ConfiguracionPage(
        DatabaseService databaseService,
        BackupService backupService,
        LicenciaService licenciaService,
        SyncService syncService)
    {
        InitializeComponent();
        _db = databaseService;
        _backup = backupService;
        _licencia = licenciaService;
        _sync = syncService;
        ListaAuditoria.ItemsSource = Auditoria;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await SessionGuard.VerificarAsync(this)) return;
#if WINDOWS
        MauiProgram.ResizarVentanaActual(520, 820);
#endif
        if (Preferences.Get(PreferenceKeys.UsuarioLogueadoRol, "") != Roles.Admin)
        {
            await DisplayAlert("Acceso", "Solo el administrador puede acceder a Ajustes.", "OK");
            await Navigation.PopAsync();
            return;
        }

        await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        var (nombre, dueno, tel, apertura, cierre) = _db.ObtenerDatosNegocio();
        EntryNombre.Text = nombre;
        EntryDueno.Text = dueno;
        EntryTelefono.Text = tel;
        if (TimeSpan.TryParse(apertura, out var ta)) PickerApertura.Time = ta;
        if (TimeSpan.TryParse(cierre, out var tc)) PickerCierre.Time = tc;

        SwitchTema.IsToggled = ThemeService.EsOscuro();
        EntryTimeout.Text = Preferences.Get(PreferenceKeys.TimeoutMinutos, 30).ToString();
        EntrySyncUrl.Text = _sync.ObtenerUrl();

        var dias = LicenciaHelper.DiasRestantes();
        var vence = Preferences.Get(PreferenceKeys.FechaVencimiento, "—");
        LblLicenciaInfo.Text = dias.HasValue ? $"Vence: {vence} · {dias} día(s) restantes" : $"Vence: {vence}";
        LblDeviceId.Text = $"Device ID: {_licencia.ObtenerDeviceId()}";

        var diasRespaldo = _backup.DiasSinRespaldo();
        if (diasRespaldo >= 3)
        {
            LblAvisoRespaldo.IsVisible = true;
            LblAvisoRespaldo.Text = $"⚠ Hace {diasRespaldo} día(s) que no respaldas tus datos.";
        }

        var sucursales = await _db.GetSucursalesAsync();
        PickerSucursal.ItemsSource = sucursales.Select(s => s.Nombre).ToList();
        var activa = Preferences.Get(PreferenceKeys.SucursalActivaId, 1);
        var idx = sucursales.FindIndex(s => s.Id == activa);
        if (idx >= 0) PickerSucursal.SelectedIndex = idx;

        Auditoria.Clear();
        foreach (var a in await _db.GetAuditoriaAsync(30))
        {
            Auditoria.Add(new AuditoriaItem
            {
                Hora = a.Timestamp.ToString("dd/MM HH:mm"),
                Texto = $"{a.Accion}: {a.Detalle} ({a.UsuarioNombre})"
            });
        }

        LblVersion.Text = $"BarberApp v{AppInfo.Current.VersionString} · Salones y estéticas";
        var ultimaSync = Preferences.Get(PreferenceKeys.SyncUltima, "");
        LblSyncEstado.Text = string.IsNullOrEmpty(ultimaSync) ? "Sin sincronizar" : $"Última sync: {ultimaSync}";
    }

    private async void OnGuardarNegocioClicked(object? sender, EventArgs e)
    {
        _db.GuardarDatosNegocio(
            EntryNombre.Text?.Trim() ?? "",
            EntryDueno.Text?.Trim() ?? "",
            EntryTelefono.Text?.Trim() ?? "",
            PickerApertura.Time.ToString(@"hh\:mm"),
            PickerCierre.Time.ToString(@"hh\:mm"));
        if (int.TryParse(EntryTimeout.Text, out var t) && t >= 0)
            Preferences.Set(PreferenceKeys.TimeoutMinutos, t);
        _sync.GuardarUrl(EntrySyncUrl.Text ?? "");
        if (PickerSucursal.SelectedIndex >= 0)
        {
            var sucursales = await _db.GetSucursalesAsync();
            if (PickerSucursal.SelectedIndex < sucursales.Count)
                _db.CambiarSucursalActiva(sucursales[PickerSucursal.SelectedIndex].Id);
        }
        await _db.RegistrarAuditoriaAsync("Config negocio", EntryNombre.Text ?? "");
        await DisplayAlert("Guardado", "Datos del negocio actualizados.", "OK");
    }

    private async void OnAgregarSucursalClicked(object? sender, EventArgs e)
    {
        var nombre = EntryNuevaSucursal.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) return;
        await _db.SaveSucursalAsync(new Sucursal { Nombre = nombre, Activa = true });
        EntryNuevaSucursal.Text = "";
        var sucursales = await _db.GetSucursalesAsync();
        PickerSucursal.ItemsSource = sucursales.Select(s => s.Nombre).ToList();
        await DisplayAlert("Sucursal", $"'{nombre}' agregada.", "OK");
    }

    private async void OnCambiarPasswordClicked(object? sender, EventArgs e)
    {
        var ok = await _db.CambiarPasswordAdminAsync(EntryPassActual.Text ?? "", EntryPassNueva.Text ?? "");
        if (ok)
        {
            EntryPassActual.Text = EntryPassNueva.Text = "";
            await DisplayAlert("Listo", "Contraseña actualizada.", "OK");
        }
        else
            await DisplayAlert("Error", "Contraseña actual incorrecta.", "OK");
    }

    private void OnTemaToggled(object? sender, ToggledEventArgs e)
    {
        ThemeService.AplicarTema(e.Value);
        ThemeService.AplicarAPagina(this);
    }

    private async void OnCopiarDeviceIdClicked(object? sender, EventArgs e)
    {
        var id = _licencia.ObtenerDeviceId();
        await Clipboard.SetTextAsync(id);
        await DisplayAlert("Copiado", "Device ID copiado al portapapeles", "OK");
    }

    private async void OnRenovarLicenciaClicked(object? sender, EventArgs e)
    {
        var deviceId = _licencia.ObtenerDeviceId();
        var licencia = await _licencia.VerificarRenovacionAsync(deviceId);
        if (licencia == null)
        {
            await DisplayAlert("Licencia", "No se encontró renovación. Contacta soporte con tu Device ID.", "OK");
            return;
        }
        var token = Preferences.Get(PreferenceKeys.LicenciaToken, licencia.Token);
        LicenciaService.GuardarPreferenciasLicencia(licencia, token, _licencia.ObtenerDeviceId());
        await DisplayAlert("Licencia", $"Renovada hasta {licencia.FechaVencimiento}", "OK");
        await CargarDatosAsync();
    }

    private async void OnSyncClicked(object? sender, EventArgs e)
    {
        _sync.GuardarUrl(EntrySyncUrl.Text ?? "");
        var (ok, msg) = await _sync.SincronizarAsync();
        await DisplayAlert(ok ? "Sync" : "Error", msg, "OK");
    }

    private async void OnExportarClicked(object? sender, EventArgs e)
    {
        try
        {
            var ruta = await _backup.GuardarRespaldoAsync();
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Exportar respaldo", File = new ShareFile(ruta) });
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private async void OnRestaurarClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccionar respaldo JSON",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, [".json"] },
                    { DevicePlatform.Android, ["application/json"] },
                    { DevicePlatform.iOS, ["public.json"] }
                })
            });
            if (result == null) return;

            var json = await File.ReadAllTextAsync(result.FullPath);
            var resumen = _backup.AnalizarRespaldo(json);
            var msg = $"Clientes: {resumen.Clientes}\nCitas: {resumen.Citas}\nCobros: {resumen.Cobros}\nProductos: {resumen.Productos}";
            var modo = await DisplayActionSheet("Modo de restauración", "Cancelar", null,
                "Fusionar (actualizar existentes)", "Reemplazar todo");
            if (modo == null || modo == "Cancelar") return;
            if (!await DisplayAlert("Confirmar", $"{msg}\n\n¿Continuar?", "Sí", "No")) return;

            await _backup.RestaurarDesdeJsonAsync(json, modo.Contains("Reemplazar"));
            await DisplayAlert("Listo", "Respaldo restaurado.", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private async void OnCopiarDbClicked(object? sender, EventArgs e)
    {
        try
        {
            var destino = Path.Combine(FileSystem.CacheDirectory, $"barberapp_{DateTime.Now:yyyyMMdd}.db3");
            await _backup.CopiarBaseDatosAsync(destino);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Copiar BD", File = new ShareFile(destino) });
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private async void OnVolverClicked(object? sender, EventArgs e) =>
        await Navigation.PopAsync();

    public class AuditoriaItem
    {
        public string Hora { get; set; } = "";
        public string Texto { get; set; } = "";
    }
}
