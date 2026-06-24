using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;
using System.Collections.ObjectModel;

namespace BarberApp.Views;

public partial class CajaPage : ContentPage
{
    private readonly DatabaseService _db;
    private readonly ReportService _report;
    private DateTime _desde = DateTime.Today;
    private DateTime _hasta = DateTime.Today;

    public ObservableCollection<CitaPendienteItem> Pendientes { get; } = new();
    public ObservableCollection<CobroItem> CobrosRecientes { get; } = new();
    public ObservableCollection<ComisionItem> Comisiones { get; } = new();

    public CajaPage(DatabaseService databaseService, ReportService reportService)
    {
        InitializeComponent();
        _db = databaseService;
        _report = reportService;
        ListaPendientes.ItemsSource = Pendientes;
        ListaCobros.ItemsSource = CobrosRecientes;
        ListaComisiones.ItemsSource = Comisiones;
        PickerDesde.Date = _desde;
        PickerHasta.Date = _hasta;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await SessionGuard.VerificarAsync(this)) return;
#if WINDOWS
        MauiProgram.ResizarVentanaActual(950, 820);
#endif
        await CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        Pendientes.Clear();
        CobrosRecientes.Clear();
        Comisiones.Clear();

        var citas = await _db.GetCitasPendientesCobroAsync(DateTime.Today);
        foreach (var c in citas)
        {
            Pendientes.Add(new CitaPendienteItem
            {
                Hora = c.Hora, NombreCliente = c.NombreCliente,
                Detalle = $"{c.NombreServicio} · {c.NombreBarbero}",
                MontoTexto = $"${c.Monto:N0}", Cita = c
            });
        }
        LblPendientes.Text = Pendientes.Count.ToString();

        var finDia = _hasta.Date.AddDays(1).AddSeconds(-1);
        var cobros = await _db.GetCobrosPorFechaAsync(_desde.Date, finDia);
        var desglose = await _db.GetDesglosePagosAsync(_desde.Date, finDia);
        var comparativa = await _db.GetComparativaSemanasAsync();
        var porBarbero = await _db.GetIngresosPorBarberoAsync(_desde.Date, finDia);
        var porServicio = await _db.GetIngresosPorServicioAsync(_desde.Date, finDia);
        var comisiones = await _db.GetReporteComisionesAsync(_desde.Date, finDia);

        LblTotalPeriodo.Text = $"${cobros.Sum(c => c.Monto):N0}";
        LblEfectivo.Text = $"${desglose[MetodosPago.Efectivo]:N0}";
        LblTarjeta.Text = $"${desglose[MetodosPago.Tarjeta]:N0}";
        LblTransferencia.Text = $"${desglose[MetodosPago.Transferencia]:N0}";

        var diff = comparativa.EstaSemana - comparativa.SemanaAnterior;
        var pct = comparativa.SemanaAnterior > 0 ? diff / comparativa.SemanaAnterior * 100 : 0;
        LblComparativa.Text = $"Semana: ${comparativa.EstaSemana:N0} vs ant. ${comparativa.SemanaAnterior:N0} ({pct:+0;-0}%)";

        LblPorBarbero.Text = porBarbero.Count > 0
            ? string.Join(" · ", porBarbero.OrderByDescending(x => x.Value).Take(3).Select(x => $"{x.Key}: ${x.Value:N0}"))
            : "—";
        LblPorServicio.Text = porServicio.Count > 0
            ? string.Join(" · ", porServicio.OrderByDescending(x => x.Value).Take(3).Select(x => $"{x.Key}: ${x.Value:N0}"))
            : "—";

        foreach (var r in comisiones)
        {
            Comisiones.Add(new ComisionItem
            {
                Barbero = r.Barbero,
                Detalle = $"{r.Cantidad} cobros · ${r.TotalCobrado:N0}",
                ComisionTexto = $"${r.Comision:N0}"
            });
        }

        foreach (var c in cobros)
        {
            CobrosRecientes.Add(new CobroItem
            {
                Descripcion = $"{c.NombreServicio} · {c.Tipo}",
                Hora = c.Timestamp.ToString("dd/MM HH:mm"),
                MontoTexto = $"+${c.Monto:N0}"
            });
        }
    }

    private async void OnFiltroHoyClicked(object? sender, EventArgs e)
    { _desde = _hasta = DateTime.Today; PickerDesde.Date = PickerHasta.Date = _desde; await CargarDatosAsync(); }

    private async void OnFiltroSemanaClicked(object? sender, EventArgs e)
    { _hasta = DateTime.Today; _desde = _hasta.AddDays(-6); PickerDesde.Date = _desde; PickerHasta.Date = _hasta; await CargarDatosAsync(); }

    private async void OnFiltroMesClicked(object? sender, EventArgs e)
    { _hasta = DateTime.Today; _desde = new DateTime(_hasta.Year, _hasta.Month, 1); PickerDesde.Date = _desde; PickerHasta.Date = _hasta; await CargarDatosAsync(); }

    private async void OnAplicarFiltroClicked(object? sender, EventArgs e)
    { _desde = PickerDesde.Date; _hasta = PickerHasta.Date; await CargarDatosAsync(); }

    private async void OnWalkInClicked(object? sender, EventArgs e)
    {
        var descripcion = await DisplayPromptAsync("Walk-in", "Descripción:", "OK", "Cancelar");
        if (string.IsNullOrWhiteSpace(descripcion)) return;
        var montoStr = await DisplayPromptAsync("Walk-in", "Monto:", "OK", "Cancelar", keyboard: Keyboard.Numeric);
        if (!ValidationHelper.EsPrecioValido(montoStr ?? "", out var monto)) return;
        var tipo = await DisplayActionSheet("Método de pago", "Cancelar", null, MetodosPago.Todos);
        if (tipo == null || tipo == "Cancelar") return;
        var barberoId = Preferences.Get(PreferenceKeys.UsuarioLogueadoId, 0);
        var barbero = Preferences.Get(PreferenceKeys.UsuarioLogueadoNombre, "Admin");
        await _db.RegistrarCobroWalkInAsync(descripcion, monto, tipo, barbero, barberoId);
        await DisplayAlert("Cobro", $"${monto:N0} registrado", "OK");
        await CargarDatosAsync();
    }

    private async void OnCitaPendienteSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CitaPendienteItem item) return;
        ListaPendientes.SelectedItem = null;
        var tipo = await DisplayActionSheet($"Cobrar ${item.Cita.Monto:N0}", "Cancelar", null, MetodosPago.Todos);
        if (tipo == null || tipo == "Cancelar") return;
        await _db.RegistrarCobroAsync(item.Cita, tipo);
        await DisplayAlert("Cobro", $"${item.Cita.Monto:N0} · {tipo}", "OK");
        await CargarDatosAsync();
    }

    private async Task ExportarAsync(string formato)
    {
        try
        {
            var finDia = _hasta.Date.AddDays(1).AddSeconds(-1);
            var cobros = await _db.GetCobrosPorFechaAsync(_desde.Date, finDia);
            var porBarbero = await _db.GetIngresosPorBarberoAsync(_desde.Date, finDia);
            var porServicio = await _db.GetIngresosPorServicioAsync(_desde.Date, finDia);
            var comparativa = await _db.GetComparativaSemanasAsync();
            var titulo = $"Reporte Caja {_desde:dd/MM/yyyy} - {_hasta:dd/MM/yyyy}";
            string ruta;

            switch (formato)
            {
                case "csv":
                    ruta = Path.Combine(FileSystem.CacheDirectory, $"caja_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                    await File.WriteAllTextAsync(ruta, _report.GenerarCsv(cobros));
                    break;
                case "excel":
                    ruta = Path.Combine(FileSystem.CacheDirectory, $"caja_{DateTime.Now:yyyyMMdd_HHmmss}.xls");
                    await File.WriteAllTextAsync(ruta, _report.GenerarExcelXml(cobros, titulo));
                    break;
                case "pdf":
                    var lineas = cobros.Select(c => $"{c.Timestamp:dd/MM HH:mm} {c.NombreServicio} {c.NombreBarbero} ${c.Monto:N0}").ToList();
                    lineas.Add($"Total: ${cobros.Sum(c => c.Monto):N0}");
                    ruta = Path.Combine(FileSystem.CacheDirectory, $"caja_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    await File.WriteAllBytesAsync(ruta, _report.GenerarPdfSimple(titulo, lineas));
                    break;
                default:
                    ruta = Path.Combine(FileSystem.CacheDirectory, $"caja_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                    await File.WriteAllTextAsync(ruta, _report.GenerarHtmlReporte(
                        titulo, cobros.Sum(c => c.Monto), porBarbero, porServicio, comparativa));
                    break;
            }

            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Exportar reporte", File = new ShareFile(ruta) });
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private async void OnExportarCsvClicked(object? sender, EventArgs e) => await ExportarAsync("csv");
    private async void OnExportarExcelClicked(object? sender, EventArgs e) => await ExportarAsync("excel");
    private async void OnExportarPdfClicked(object? sender, EventArgs e) => await ExportarAsync("pdf");
    private async void OnVolverClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    public class CitaPendienteItem
    {
        public string Hora { get; set; } = "";
        public string NombreCliente { get; set; } = "";
        public string Detalle { get; set; } = "";
        public string MontoTexto { get; set; } = "";
        public Cita Cita { get; set; } = null!;
    }

    public class CobroItem
    {
        public string Descripcion { get; set; } = "";
        public string Hora { get; set; } = "";
        public string MontoTexto { get; set; } = "";
    }

    public class ComisionItem
    {
        public string Barbero { get; set; } = "";
        public string Detalle { get; set; } = "";
        public string ComisionTexto { get; set; } = "";
    }
}
