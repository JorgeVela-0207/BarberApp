using BarberApp.Models;
using System.Text.Json;

namespace BarberApp.Services;

/// <summary>
/// Copia local de la licencia (misma carpeta que la BD). Sobrevive si Preferences se resetean al reinstalar MSIX en debug.
/// </summary>
public static class LicenciaLocalStore
{
    private static string RutaArchivo =>
        Path.Combine(FileSystem.AppDataDirectory, "licencia.local.json");

    public class LicenciaLocalData
    {
        public string Token { get; set; } = string.Empty;
        public string FechaVencimiento { get; set; } = string.Empty;
        public string NombreLocal { get; set; } = string.Empty;
        public string Dueno { get; set; } = string.Empty;
        public string IdNegocio { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
    }

    public static void Guardar(string token, LicenciaDto licencia, string deviceId)
    {
        var data = new LicenciaLocalData
        {
            Token = token,
            FechaVencimiento = licencia.FechaVencimiento,
            NombreLocal = licencia.NombreLocal,
            Dueno = licencia.Dueno,
            IdNegocio = licencia.IdNegocio,
            DeviceId = deviceId
        };

        try
        {
            File.WriteAllText(RutaArchivo, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* ignore */ }
    }

    public static LicenciaLocalData? Cargar()
    {
        try
        {
            if (!File.Exists(RutaArchivo))
                return null;
            return JsonSerializer.Deserialize<LicenciaLocalData>(File.ReadAllText(RutaArchivo));
        }
        catch
        {
            return null;
        }
    }

    public static void AplicarAPreferencias(LicenciaLocalData data)
    {
        Preferences.Set(PreferenceKeys.LicenciaActivada, true);
        Preferences.Set(PreferenceKeys.LicenciaToken, data.Token);
        Preferences.Set(PreferenceKeys.FechaVencimiento, data.FechaVencimiento);
        Preferences.Set(PreferenceKeys.NombreLocal, data.NombreLocal);
        Preferences.Set(PreferenceKeys.Dueno, data.Dueno);
        Preferences.Set(PreferenceKeys.IdNegocio, data.IdNegocio);
    }

    public static bool EsValidaLocalmente(LicenciaLocalData data) =>
        !string.IsNullOrWhiteSpace(data.Token) &&
        !Core.LicenciaDateHelper.EstaVencida(data.FechaVencimiento, DateTime.Today);
}
