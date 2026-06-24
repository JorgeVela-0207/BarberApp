using BarberApp.Models;
using System.Text;
using System.Text.Json;

namespace BarberApp.Services;

public class SyncService
{
    private readonly DatabaseService _db;
    private readonly DeviceIdService _deviceId;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public SyncService(DatabaseService databaseService, DeviceIdService deviceIdService)
    {
        _db = databaseService;
        _deviceId = deviceIdService;
    }

    public string ObtenerUrl() => Preferences.Get(PreferenceKeys.SyncUrl, "");

    public void GuardarUrl(string url) => Preferences.Set(PreferenceKeys.SyncUrl, url.Trim());

    public async Task<(bool Ok, string Mensaje)> SincronizarAsync()
    {
        var url = ObtenerUrl();
        if (string.IsNullOrWhiteSpace(url))
            return (false, "Configura la URL de sincronización en Ajustes.");

        var pendientes = await _db.GetPendientesSyncAsync();
        if (pendientes.Count == 0)
            return (true, "No hay cambios pendientes.");

        try
        {
            var payload = new
            {
                sucursal_id = _db.SucursalActivaId,
                device_id = _deviceId.ObtenerDeviceId(),
                cambios = pendientes.Select(p => new
                {
                    p.Entidad,
                    p.EntidadId,
                    p.Operacion,
                    p.PayloadJson,
                    p.Timestamp
                })
            };

            var json = JsonSerializer.Serialize(payload);
            var response = await Http.PostAsync(
                url.TrimEnd('/') + "/push",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                return (false, $"Error del servidor: {(int)response.StatusCode}");

            await _db.MarcarSincronizadoAsync(pendientes.Select(p => p.Id));
            Preferences.Set(PreferenceKeys.SyncUltima, DateTime.Now.ToString("O"));
            return (true, $"{pendientes.Count} cambio(s) sincronizado(s).");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
