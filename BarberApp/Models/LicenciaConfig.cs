namespace BarberApp.Models;

/// <summary>
/// Configuración del servicio de licencias. Para producción usa repo privado o API propia.
/// </summary>
public static class LicenciaConfig
{
    /// <summary>
    /// URL del JSON de licencias. Debe ser accesible desde la app del cliente (repo público o API).
    /// Configuración: docs/PASO_A_PASO_LICENCIAS.md
    /// </summary>
    public const string Url =
        "https://raw.githubusercontent.com/JorgeVela-0207/BDLicencias/main/licencias.json";

    /// <summary>
    /// Token opcional (PAT de GitHub u otro bearer) para acceder a un repo privado.
    /// Déjalo vacío si el endpoint es público. En CI puedes inyectarlo con -p:LicenciaApiToken=...
    /// </summary>
    public static string ApiToken =>
        string.IsNullOrWhiteSpace(_apiTokenOverride) ? "" : _apiTokenOverride;

    private static string _apiTokenOverride = "";

    public static void ConfigurarApiToken(string? token) =>
        _apiTokenOverride = token?.Trim() ?? "";
}
