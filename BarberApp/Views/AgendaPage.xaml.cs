using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;
using System.Collections.ObjectModel;

namespace BarberApp.Views;

public partial class AgendaPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly int _barberoLogueadoId;
    private readonly string _rol;
    private DateTime _fechaActual = DateTime.Today;
    private int _citaEditandoId;
    private bool _vistaSemana;

    private List<Cliente> _clientes = [];
    private List<Barbero> _barberos = [];
    private List<Servicio> _servicios = [];

    public ObservableCollection<CitaItem> Citas { get; } = new();

    public AgendaPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _db = databaseService;
        _rol = Preferences.Get(PreferenceKeys.UsuarioLogueadoRol, "");
        _barberoLogueadoId = Preferences.Get(PreferenceKeys.UsuarioLogueadoId, 0);
        ListaCitas.ItemsSource = Citas;
        PickerFecha.Date = _fechaActual;
        PickerServicio.SelectedIndexChanged += (_, _) => ActualizarMontoPreview();
        PickerRecurrencia.ItemsSource = new[] { "Sin repetir", "Cada 15 días", "Mensual" };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await SessionGuard.VerificarAsync(this)) return;
#if WINDOWS
        MauiProgram.ResizarVentanaActual(900, 780);
#endif
        await CargarDatosAsync();
        await CargarCitasAsync();
    }

    private async Task CargarDatosAsync()
    {
        _clientes = await _db.GetClientesAsync();
        _barberos = await _db.GetBarberosAsync();
        _servicios = await _db.GetServiciosAsync();
        PickerCliente.ItemsSource = _clientes.Select(c => c.Nombre).ToList();
        PickerBarbero.ItemsSource = _barberos.Select(b => b.Nombre).ToList();
        PickerServicio.ItemsSource = _servicios.Select(s => $"{s.Nombre} (${s.Precio:N0})").ToList();
        if (_rol != Roles.Admin)
        {
            PickerBarbero.IsEnabled = false;
            var idx = _barberos.FindIndex(b => b.Id == _barberoLogueadoId);
            if (idx >= 0) PickerBarbero.SelectedIndex = idx;
        }
    }

    private async Task CargarCitasAsync()
    {
        Citas.Clear();
        List<Cita> citas;

        if (_vistaSemana)
        {
            var fin = _fechaActual.AddDays(6);
            LblFechaAgenda.Text = $"{_fechaActual:dd MMM} — {fin:dd MMM yyyy}";
            citas = _rol == Roles.Admin
                ? await _db.GetCitasPorRangoAsync(_fechaActual, fin)
                : (await _db.GetCitasPorRangoAsync(_fechaActual, fin))
                    .Where(c => c.BarberoId == _barberoLogueadoId).ToList();
        }
        else
        {
            LblFechaAgenda.Text = _fechaActual.ToString("dddd dd MMM yyyy", new System.Globalization.CultureInfo("es-MX"));
            citas = _rol == Roles.Admin
                ? await _db.GetCitasPorFechaAsync(_fechaActual)
                : await _db.GetCitasPorFechaYBarberoAsync(_fechaActual, _barberoLogueadoId);
        }

        foreach (var c in citas)
        {
            var (fondo, borde, estado) = c.Estado switch
            {
                CitaEstados.Cobrada => (ThemeService.GetColorHex("AccentGreenSoft"), ThemeService.GetColorHex("AccentGreen"), "COBRADA"),
                CitaEstados.Cancelada => ("#2a1a1a", "#4a2a2a", "CANCELADA"),
                _ => (ThemeService.GetColorHex("CardBackground"), ThemeService.GetColorHex("AccentGold"), "PENDIENTE")
            };
            Citas.Add(new CitaItem
            {
                Id = c.Id,
                Hora = c.Hora,
                FechaCorta = c.Fecha.ToString("ddd dd"),
                NombreCliente = c.NombreCliente,
                Detalle = $"{c.NombreServicio} · {c.NombreBarbero} · ${c.Monto:N0}",
                EstadoTexto = estado,
                ColorFondo = fondo,
                ColorBorde = borde,
                Cita = c
            });
        }
    }

    private void ActualizarVistaBotones()
    {
        var gold = ThemeService.GetColor("AccentGold");
        var card = ThemeService.GetColor("CardBackground");
        var textSec = ThemeService.GetColor("TextSecondary");
        var onAccent = ThemeService.GetColor("ButtonOnAccent");

        BtnVistaDia.BackgroundColor = !_vistaSemana ? gold : card;
        BtnVistaDia.TextColor = !_vistaSemana ? onAccent : textSec;
        BtnVistaSemana.BackgroundColor = _vistaSemana ? gold : card;
        BtnVistaSemana.TextColor = _vistaSemana ? onAccent : textSec;
    }

    private void ActualizarMontoPreview()
    {
        if (PickerServicio.SelectedIndex >= 0 && PickerServicio.SelectedIndex < _servicios.Count)
            LblMontoPreview.Text = $"${_servicios[PickerServicio.SelectedIndex].Precio:N0}";
        else LblMontoPreview.Text = "$0";
    }

    private async void OnVistaDiaClicked(object? sender, EventArgs e) { _vistaSemana = false; ActualizarVistaBotones(); await CargarCitasAsync(); }
    private async void OnVistaSemanaClicked(object? sender, EventArgs e) { _vistaSemana = true; ActualizarVistaBotones(); await CargarCitasAsync(); }

    private async void OnPeriodoAnteriorClicked(object? sender, EventArgs e)
    {
        _fechaActual = _vistaSemana ? _fechaActual.AddDays(-7) : _fechaActual.AddDays(-1);
        PickerFecha.Date = _fechaActual;
        await CargarCitasAsync();
    }

    private async void OnPeriodoSiguienteClicked(object? sender, EventArgs e)
    {
        _fechaActual = _vistaSemana ? _fechaActual.AddDays(7) : _fechaActual.AddDays(1);
        PickerFecha.Date = _fechaActual;
        await CargarCitasAsync();
    }

    private async void OnFechaChanged(object? sender, DateChangedEventArgs e)
    {
        _fechaActual = e.NewDate;
        await CargarCitasAsync();
    }

    private async void OnNuevaCitaClicked(object? sender, EventArgs e)
    {
        if (_clientes.Count == 0 || _servicios.Count == 0 || _barberos.Count == 0)
        {
            await DisplayAlert("Datos incompletos", "Agrega clientes, servicios y personal en Catálogos.", "OK");
            return;
        }
        _citaEditandoId = 0;
        LblFormCita.Text = "Nueva cita";
        PickerCliente.SelectedIndex = 0;
        PickerServicio.SelectedIndex = 0;
        PickerRecurrencia.SelectedIndex = 0;
        if (_rol == Roles.Admin) PickerBarbero.SelectedIndex = 0;
        PickerHora.Time = DateTime.Now.TimeOfDay;
        ActualizarMontoPreview();
        PanelCita.IsVisible = true;
    }

    private async void OnCitaSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CitaItem item) return;
        ListaCitas.SelectedItem = null;
        var cita = item.Cita;

        if (cita.Estado == CitaEstados.Cobrada) { await DisplayAlert("Cita", "Ya fue cobrada.", "OK"); return; }
        if (cita.Estado == CitaEstados.Cancelada) { await DisplayAlert("Cita", "Está cancelada.", "OK"); return; }

        var accion = await DisplayActionSheet($"{cita.Hora} · {cita.NombreCliente}", "Cerrar", null,
            "Editar", "Cobrar ahora", "Enviar WhatsApp", "Cancelar cita");

        if (accion == "Editar")
        {
            _citaEditandoId = cita.Id;
            LblFormCita.Text = "Editar cita";
            PickerCliente.SelectedIndex = _clientes.FindIndex(c => c.Id == cita.ClienteId);
            PickerBarbero.SelectedIndex = _barberos.FindIndex(b => b.Id == cita.BarberoId);
            PickerServicio.SelectedIndex = _servicios.FindIndex(s => s.Id == cita.ServicioId);
            if (CitaHorarioHelper.ParseHora(cita.Hora, out var hora)) PickerHora.Time = hora;
            ActualizarMontoPreview();
            PanelCita.IsVisible = true;
        }
        else if (accion == "Cobrar ahora")
        {
            var tipo = await DisplayActionSheet("Método de pago", "Cancelar", null, MetodosPago.Todos);
            if (tipo == null || tipo == "Cancelar") return;
            await _db.RegistrarCobroAsync(cita, tipo);
            await DisplayAlert("Cobro", $"${cita.Monto:N0} registrado", "OK");
            await CargarCitasAsync();
        }
        else if (accion == "Enviar WhatsApp")
        {
            var cliente = _clientes.FirstOrDefault(c => c.Id == cita.ClienteId);
            var negocio = Preferences.Get(PreferenceKeys.NombreLocal, "BarberApp");
            var msg = $"Hola {cita.NombreCliente}, te recordamos tu cita en {negocio} el {cita.Fecha:dd/MM} a las {cita.Hora}. Servicio: {cita.NombreServicio}.";
            await ShareHelper.CompartirCitaWhatsAppAsync(new ShareHelper.CitaInfo(cliente?.Telefono ?? "", msg));
        }
        else if (accion == "Cancelar cita" && await DisplayAlert("Confirmar", "¿Cancelar?", "Sí", "No"))
        {
            await _db.CancelarCitaAsync(cita);
            await CargarCitasAsync();
        }
    }

    private async void OnGuardarCitaClicked(object? sender, EventArgs e)
    {
        if (PickerCliente.SelectedIndex < 0 || PickerBarbero.SelectedIndex < 0 || PickerServicio.SelectedIndex < 0)
        {
            await DisplayAlert("Error", "Completa todos los campos.", "OK");
            return;
        }

        var cliente = _clientes[PickerCliente.SelectedIndex];
        var barbero = _barberos[PickerBarbero.SelectedIndex];
        var servicio = _servicios[PickerServicio.SelectedIndex];
        var hora = CitaHorarioHelper.FormatearHora(PickerHora.Time);

        if (await _db.HayConflictoCitaAsync(barbero.Id, _fechaActual, hora, servicio.DuracionMinutos, _citaEditandoId))
        {
            await DisplayAlert("Horario ocupado", "Conflicto con otra cita o bloqueo de horario.", "OK");
            return;
        }

        var recurrencia = PickerRecurrencia.SelectedIndex switch
        {
            1 => RecurrenciaTipos.Quincenal,
            2 => RecurrenciaTipos.Mensual,
            _ => RecurrenciaTipos.Ninguna
        };

        Cita cita = _citaEditandoId > 0
            ? await _db.GetCitaByIdAsync(_citaEditandoId) ?? new Cita()
            : new Cita { Estado = CitaEstados.Pendiente };

        cita.ClienteId = cliente.Id;
        cita.BarberoId = barbero.Id;
        cita.ServicioId = servicio.Id;
        cita.Fecha = _fechaActual.Date;
        cita.Hora = hora;
        cita.NombreCliente = cliente.Nombre;
        cita.NombreBarbero = barbero.Nombre;
        cita.NombreServicio = servicio.Nombre;
        cita.Monto = servicio.Precio;
        cita.DuracionMinutos = servicio.DuracionMinutos;
        cita.Estado = CitaEstados.Pendiente;
        cita.RecurrenciaTipo = recurrencia;
        if (recurrencia != RecurrenciaTipos.Ninguna && string.IsNullOrEmpty(cita.RecurrenciaGrupoId))
            cita.RecurrenciaGrupoId = Guid.NewGuid().ToString("N");

        await _db.SaveCitaAsync(cita);
        PanelCita.IsVisible = false;
        _citaEditandoId = 0;
        await CargarCitasAsync();
    }

    private async void OnWhatsAppMasivoClicked(object? sender, EventArgs e)
    {
        var citas = await _db.GetCitasPendientesConTelefonoAsync(_fechaActual);
        if (citas.Count == 0) { await DisplayAlert("WhatsApp", "No hay citas pendientes hoy.", "OK"); return; }
        if (!await DisplayAlert("WhatsApp masivo", $"¿Enviar recordatorio a {citas.Count} cliente(s)?", "Sí", "No")) return;

        var negocio = Preferences.Get(PreferenceKeys.NombreLocal, "BarberApp");
        foreach (var c in citas)
        {
            var cl = _clientes.FirstOrDefault(x => x.Id == c.ClienteId);
            if (cl == null || string.IsNullOrWhiteSpace(cl.Telefono)) continue;
            var msg = $"Hola {c.NombreCliente}, recordatorio de tu cita en {negocio} hoy a las {c.Hora}. Servicio: {c.NombreServicio}.";
            await ShareHelper.CompartirCitaWhatsAppAsync(new ShareHelper.CitaInfo(cl.Telefono, msg));
            await Task.Delay(800);
        }
        await DisplayAlert("Listo", "Recordatorios enviados.", "OK");
    }

    private async void OnBloqueosClicked(object? sender, EventArgs e)
    {
        var accion = await DisplayActionSheet("Bloqueos de horario", "Cerrar", null, "Nuevo bloqueo", "Ver bloqueos");
        if (accion == "Nuevo bloqueo") await CrearBloqueoAsync();
        else if (accion == "Ver bloqueos") await VerBloqueosAsync();
    }

    private async Task CrearBloqueoAsync()
    {
        var motivo = await DisplayPromptAsync("Bloqueo", "Motivo (descanso, cierre...):", "OK", "Cancelar");
        if (string.IsNullOrWhiteSpace(motivo)) return;
        var hi = await DisplayPromptAsync("Bloqueo", "Hora inicio (HH:mm):", "OK", "Cancelar", initialValue: "14:00");
        var hf = await DisplayPromptAsync("Bloqueo", "Hora fin (HH:mm):", "OK", "Cancelar", initialValue: "15:00");
        var tipo = await DisplayActionSheet("Tipo", "Cancelar", null, "Hoy", "Todos los días de la semana");
        if (tipo == null || tipo == "Cancelar") return;

        var bloqueo = new BloqueoHorario
        {
            BarberoId = _rol == Roles.Admin ? 0 : _barberoLogueadoId,
            Motivo = motivo,
            HoraInicio = hi ?? "14:00",
            HoraFin = hf ?? "15:00",
            FechaEspecifica = tipo == "Hoy" ? _fechaActual.Date : null,
            DiaSemana = tipo == "Hoy" ? -1 : (int)_fechaActual.DayOfWeek
        };
        await _db.SaveBloqueoAsync(bloqueo);
        await _db.RegistrarAuditoriaAsync("Bloqueo horario", motivo);
        await DisplayAlert("Bloqueo", "Horario bloqueado.", "OK");
    }

    private async Task VerBloqueosAsync()
    {
        var bloqueos = await _db.GetBloqueosAsync();
        if (bloqueos.Count == 0) { await DisplayAlert("Bloqueos", "Sin bloqueos.", "OK"); return; }
        var texto = string.Join("\n", bloqueos.Select(b =>
            $"• {b.Motivo}: {b.HoraInicio}-{b.HoraFin} {(b.FechaEspecifica.HasValue ? b.FechaEspecifica.Value.ToString("dd/MM") : "recurrente")}"));
        await DisplayAlert("Bloqueos activos", texto, "OK");
    }

    private void OnCancelarCitaFormClicked(object? sender, EventArgs e) { PanelCita.IsVisible = false; _citaEditandoId = 0; }
    private async void OnVolverClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    public class CitaItem
    {
        public int Id { get; set; }
        public string Hora { get; set; } = "";
        public string FechaCorta { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string Detalle { get; set; } = "";
        public string EstadoTexto { get; set; } = "";
        public string ColorFondo { get; set; } = "#16161a";
        public string ColorBorde { get; set; } = "#C4A77D";
        public Cita Cita { get; set; } = null!;
    }
}
