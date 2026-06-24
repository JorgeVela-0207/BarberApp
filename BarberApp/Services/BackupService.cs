using BarberApp.Models;
using System.Text.Json;

namespace BarberApp.Services;

public class BackupService
{
    private readonly DatabaseService _db;

    public BackupService(DatabaseService databaseService)
    {
        _db = databaseService;
    }

    public async Task VerificarRespaldoAutomaticoAsync()
    {
        var ultimo = Preferences.Get(PreferenceKeys.UltimoRespaldo, "");
        var necesita = string.IsNullOrEmpty(ultimo) ||
                       !DateTime.TryParse(ultimo, out var dt) ||
                       DateTime.Now - dt > TimeSpan.FromHours(24);

        if (!necesita) return;

        var dir = Path.Combine(FileSystem.AppDataDirectory, "backups");
        Directory.CreateDirectory(dir);
        var ruta = Path.Combine(dir, $"auto_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        var json = await ExportarJsonAsync();
        await File.WriteAllTextAsync(ruta, json);
        Preferences.Set(PreferenceKeys.UltimoRespaldo, DateTime.Now.ToString("O"));

        // Mantener solo últimos 7 respaldos
        var archivos = Directory.GetFiles(dir, "auto_*.json")
            .OrderByDescending(f => f).Skip(7).ToList();
        foreach (var f in archivos)
        {
            try { File.Delete(f); } catch { /* ignore */ }
        }
    }

    public int DiasSinRespaldo()
    {
        var ultimo = Preferences.Get(PreferenceKeys.UltimoRespaldo, "");
        if (!DateTime.TryParse(ultimo, out var dt)) return 999;
        return (int)(DateTime.Now - dt).TotalDays;
    }

    public async Task<string> ExportarJsonAsync()
    {
        var backup = new BackupData
        {
            ExportadoEn = DateTime.Now,
            Clientes = await _db.GetClientesAsync(),
            Barberos = await _db.GetAllBarberosAsync(),
            Servicios = await _db.GetAllServiciosAsync(),
            Citas = await _db.GetAllCitasAsync(),
            Cobros = await _db.GetAllCobrosAsync(),
            Productos = await _db.GetAllProductosAsync(),
            Sucursales = await _db.GetSucursalesAsync(),
            Bloqueos = await _db.GetBloqueosAsync()
        };
        return JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
    }

    public BackupResumen AnalizarRespaldo(string json)
    {
        var backup = JsonSerializer.Deserialize<BackupData>(json)
            ?? throw new InvalidOperationException("Archivo inválido.");
        return new BackupResumen
        {
            Clientes = backup.Clientes.Count,
            Barberos = backup.Barberos.Count,
            Servicios = backup.Servicios.Count,
            Citas = backup.Citas.Count,
            Cobros = backup.Cobros.Count,
            Productos = backup.Productos.Count,
            ExportadoEn = backup.ExportadoEn
        };
    }

    public async Task<string> GuardarRespaldoAsync()
    {
        var json = await ExportarJsonAsync();
        var nombre = $"barberapp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var ruta = Path.Combine(FileSystem.AppDataDirectory, nombre);
        await File.WriteAllTextAsync(ruta, json);
        Preferences.Set(PreferenceKeys.UltimoRespaldo, DateTime.Now.ToString("O"));
        return ruta;
    }

    public async Task CopiarBaseDatosAsync(string destino)
    {
        var origen = DatabaseService.DatabasePath;
        if (!File.Exists(origen))
            throw new FileNotFoundException("No se encontró la base de datos.");
        await Task.Run(() => File.Copy(origen, destino, true));
    }

    public async Task RestaurarDesdeJsonAsync(string json, bool reemplazarTodo)
    {
        var backup = JsonSerializer.Deserialize<BackupData>(json)
            ?? throw new InvalidOperationException("Archivo de respaldo inválido.");

        if (reemplazarTodo)
            await _db.LimpiarDatosSucursalAsync();

        foreach (var c in backup.Clientes)
            await _db.SaveClienteAsync(c);
        foreach (var s in backup.Servicios)
            await _db.SaveServicioAsync(s);
        foreach (var b in backup.Barberos)
            await _db.SaveBarberoAsync(b);
        foreach (var c in backup.Citas)
            await _db.SaveCitaAsync(c, generarRecurrencia: false);
        foreach (var c in backup.Cobros)
            await _db.RestaurarCobroAsync(c);
        foreach (var p in backup.Productos)
            await _db.SaveProductoAsync(p);

        await _db.RegistrarAuditoriaAsync("Restaurar respaldo",
            reemplazarTodo ? "Reemplazo total" : "Fusión");
    }

    private class BackupData
    {
        public DateTime ExportadoEn { get; set; }
        public List<Cliente> Clientes { get; set; } = [];
        public List<Barbero> Barberos { get; set; } = [];
        public List<Servicio> Servicios { get; set; } = [];
        public List<Cita> Citas { get; set; } = [];
        public List<Cobro> Cobros { get; set; } = [];
        public List<Producto> Productos { get; set; } = [];
        public List<Sucursal> Sucursales { get; set; } = [];
        public List<BloqueoHorario> Bloqueos { get; set; } = [];
    }
}
