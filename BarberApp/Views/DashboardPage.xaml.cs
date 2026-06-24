using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;
using System.Collections.ObjectModel;

namespace BarberApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly LicenciaService _licenciaService;
    private readonly BackupService _backupService;
    private readonly NotificationService _notificationService;
    private readonly SyncService _syncService;

    public ObservableCollection<PendienteRapidoItem> PendientesRapidos { get; } = new();

    public DashboardPage(
        DatabaseService databaseService,
        LicenciaService licenciaService,
        BackupService backupService,
        NotificationService notificationService,
        SyncService syncService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _licenciaService = licenciaService;
        _backupService = backupService;
        _notificationService = notificationService;
        _syncService = syncService;
        ListaPendientesRapida.ItemsSource = PendientesRapidos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await SessionGuard.VerificarAsync(this)) return;
#if WINDOWS
        MauiProgram.ResizarVentanaActual(1100, 800);
#endif
        await _backupService.VerificarRespaldoAutomaticoAsync();
        await _notificationService.ProgramarRecordatoriosAsync();
        _ = _syncService.SincronizarAsync();
        await CargarDashboardAsync();
    }

    private async Task CargarDashboardAsync()
    {
        var rol = Preferences.Get(PreferenceKeys.UsuarioLogueadoRol, "");
        var barberoId = Preferences.Get(PreferenceKeys.UsuarioLogueadoId, 0);
        var esAdmin = rol == Roles.Admin;

        LblNombreNegocio.Text = Preferences.Get(PreferenceKeys.NombreLocal, "Mi negocio");
        LblUsuario.Text = Preferences.Get(PreferenceKeys.UsuarioLogueadoNombre, "Usuario");
        LblFecha.Text = DateTime.Today.ToString("ddd dd MMM yyyy",
            new System.Globalization.CultureInfo("es-MX")).ToUpper();

        MostrarAvisoLicencia();

        List<Cita> citasHoy = esAdmin
            ? await _databaseService.GetCitasPorFechaAsync(DateTime.Today)
            : await _databaseService.GetCitasPorFechaYBarberoAsync(DateTime.Today, barberoId);

        var pendientes = citasHoy.Count(c => c.Estado == CitaEstados.Pendiente);
        var terminadas = citasHoy.Count(c => c.Estado == CitaEstados.Cobrada);

        if (esAdmin)
        {
            StatsAdmin.IsVisible = true;
            StatsBarbero.IsVisible = false;
            TarjetasAdmin.IsVisible = true;
            TarjetasBarbero.IsVisible = false;
            PanelSemana.IsVisible = true;
            PanelBarberoInfo.IsVisible = false;

            LblPendientesAdmin.Text = pendientes.ToString();
            LblTerminadasAdmin.Text = terminadas.ToString();

            var cobros = await _databaseService.GetCobrosPorFechaAsync(
                DateTime.Today, DateTime.Today.AddDays(1).AddSeconds(-1));
            LblCobradoAdmin.Text = $"${cobros.Sum(c => c.Monto):N0}";

            await CargarGraficaSemanaAsync();
        }
        else
        {
            StatsAdmin.IsVisible = false;
            StatsBarbero.IsVisible = true;
            TarjetasAdmin.IsVisible = false;
            TarjetasBarbero.IsVisible = true;
            PanelSemana.IsVisible = false;
            PanelBarberoInfo.IsVisible = true;

            LblPendientesBarbero.Text = pendientes.ToString();
            LblTerminadasBarbero.Text = terminadas.ToString();
        }

        var proxima = await _databaseService.GetProximaCitaAsync(esAdmin ? null : barberoId);
        LblProximaCita.Text = proxima == null
            ? "Agenda libre — crea una cita en Agenda"
            : $"{proxima.Hora}  ·  {proxima.NombreCliente}\n{proxima.NombreServicio}";

        var proximaHora = await _databaseService.GetCitasProximaHoraAsync(esAdmin ? null : barberoId);
        if (proximaHora.Count > 0)
        {
            PanelRecordatorio.IsVisible = true;
            LblRecordatorio.Text = $"⏰ {proximaHora.Count} cita(s) en la próxima hora";
        }
        else
        {
            PanelRecordatorio.IsVisible = false;
        }

        PendientesRapidos.Clear();
        var pendientesLista = await _databaseService.GetCitasPendientesHoyAsync(esAdmin ? null : barberoId);
        foreach (var c in pendientesLista.Take(3))
        {
            PendientesRapidos.Add(new PendienteRapidoItem
            {
                Hora = c.Hora,
                Cliente = c.NombreCliente,
                Servicio = c.NombreServicio
            });
        }

        PanelPendientes.IsVisible = PendientesRapidos.Count > 0;
        ListaPendientesRapida.HeightRequest = PendientesRapidos.Count switch
        {
            1 => 28,
            2 => 48,
            _ => 52
        };
    }

    private void MostrarAvisoLicencia()
    {
        if (LicenciaHelper.DebeMostrarAviso() && LicenciaHelper.DiasRestantes() is int dias)
        {
            BannerLicencia.IsVisible = true;
            LblAvisoLicencia.Text = dias == 0
                ? "Tu licencia vence hoy. Renueva para evitar interrupciones."
                : $"Tu licencia vence en {dias} día(s).";
        }
        else
        {
            BannerLicencia.IsVisible = false;
        }
    }

    private async Task CargarGraficaSemanaAsync()
    {
        FlexSemana.Children.Clear();
        var ingresos = await _databaseService.GetIngresosSemanaAsync();
        var max = ingresos.Values.DefaultIfEmpty(0).Max();
        if (max == 0) max = 1;

        foreach (var (dia, monto) in ingresos)
        {
            var altura = Math.Max(6, (double)(monto / max) * 52);
            FlexSemana.Children.Add(new VerticalStackLayout
            {
                WidthRequest = 32,
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new BoxView
                    {
                        HeightRequest = altura,
                        WidthRequest = 18,
                        Color = monto > 0 ? ThemeService.GetColor("AccentLavender") : ThemeService.GetColor("CardBorder"),
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.End,
                        CornerRadius = 4
                    },
                    new Label
                    {
                        Text = dia.ToString("ddd")[..2].ToUpper(),
                        FontSize = 9,
                        TextColor = ThemeService.GetColor("TextMuted"),
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            });
        }
    }

    private async void OnAgendaTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(PageFactory.Agenda());

    private async void OnCajaTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(PageFactory.Caja());

    private async void OnCatalogosTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(PageFactory.Catalogos());

    private async void OnAjustesTapped(object? sender, TappedEventArgs e)
    {
        if (Preferences.Get(PreferenceKeys.UsuarioLogueadoRol, "") != Roles.Admin)
        {
            await DisplayAlert("Acceso", "Solo el administrador puede acceder a Ajustes.", "OK");
            return;
        }
        await Navigation.PushAsync(PageFactory.Configuracion());
    }

    private void OnLogoutTapped(object? sender, TappedEventArgs e)
    {
        SessionService.CerrarSesion();
        NavigationHelper.SetRootPage(PageFactory.Login());
    }

    public class PendienteRapidoItem
    {
        public string Hora { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Servicio { get; set; } = "";
    }
}
