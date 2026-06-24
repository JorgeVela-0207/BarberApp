using System.Text;

using System.Text.Json;

using System.Text.Json.Serialization;

using BarberApp.Core;

using BarberApp.Helpers;

using BarberApp.Models;



namespace BarberApp.Services;



public class LicenciaDto

{

    [JsonPropertyName("id_negocio")]

    public string IdNegocio { get; set; } = string.Empty;



    [JsonPropertyName("nombre_local")]

    public string NombreLocal { get; set; } = string.Empty;



    [JsonPropertyName("dueno")]

    public string Dueno { get; set; } = string.Empty;



    [JsonPropertyName("dispositivo_id")]

    public string DispositivoId { get; set; } = string.Empty;



    [JsonPropertyName("token")]

    public string Token { get; set; } = string.Empty;



    [JsonPropertyName("estado")]

    public string Estado { get; set; } = string.Empty;



    [JsonPropertyName("fecha_activacion")]

    public string FechaActivacion { get; set; } = string.Empty;



    [JsonPropertyName("fecha_vencimiento")]

    public string FechaVencimiento { get; set; } = string.Empty;

}



public enum ResultadoActivacionLicencia

{

    Exito,

    SinConexion,

    TokenInvalido,

    DispositivoNoRegistrado,

    LicenciaVencida,

    LicenciaInactiva

}



public class LicenciaService

{

    private readonly DeviceIdService _deviceIdService;

    private static readonly HttpClient HttpClient = CreateHttpClient();



    public LicenciaService(DeviceIdService deviceIdService)

    {

        _deviceIdService = deviceIdService;

    }



    public string ObtenerDeviceId() => _deviceIdService.ObtenerDeviceId();



    public async Task<(ResultadoActivacionLicencia Resultado, LicenciaDto? Licencia)> ActivarAsync(string token)

    {

        var deviceId = ObtenerDeviceId();

        var licencias = await ObtenerLicenciasAsync();

        if (licencias == null)

            return (ResultadoActivacionLicencia.SinConexion, null);



        var licencia = licencias.FirstOrDefault(l =>

            string.Equals(l.Token, token.Trim(), StringComparison.OrdinalIgnoreCase));



        if (licencia == null)

            return (ResultadoActivacionLicencia.TokenInvalido, null);



        if (!string.Equals(licencia.Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase))

            return (ResultadoActivacionLicencia.LicenciaInactiva, licencia);



        if (!LicenciaDateHelper.EsFechaValida(licencia.FechaVencimiento, DateTime.Today))

            return (ResultadoActivacionLicencia.LicenciaVencida, licencia);

        if (!string.IsNullOrWhiteSpace(licencia.DispositivoId) &&
            !string.Equals(licencia.DispositivoId, deviceId, StringComparison.OrdinalIgnoreCase))
            return (ResultadoActivacionLicencia.DispositivoNoRegistrado, licencia);

        return (ResultadoActivacionLicencia.Exito, licencia);

    }



    public async Task<LicenciaDto?> ValidarLicenciaAsync(string deviceId, string token)

    {

        var (resultado, licencia) = await ActivarAsync(token);

        return resultado == ResultadoActivacionLicencia.Exito ? licencia : null;

    }



    public async Task<LicenciaDto?> VerificarRenovacionAsync(string deviceId)

    {

        var licencias = await ObtenerLicenciasAsync();

        if (licencias == null)

            return null;



        var tokenGuardado = Preferences.Get(PreferenceKeys.LicenciaToken, string.Empty);



        var porDevice = licencias.FirstOrDefault(l =>

            string.Equals(l.DispositivoId, deviceId, StringComparison.OrdinalIgnoreCase) &&

            string.Equals(l.Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase) &&

            LicenciaDateHelper.EsFechaValida(l.FechaVencimiento, DateTime.Today));



        if (porDevice != null)

            return porDevice;



        if (!string.IsNullOrEmpty(tokenGuardado))

        {

            return licencias.FirstOrDefault(l =>

                string.Equals(l.Token, tokenGuardado, StringComparison.OrdinalIgnoreCase) &&

                string.Equals(l.Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase) &&

                LicenciaDateHelper.EsFechaValida(l.FechaVencimiento, DateTime.Today));

        }



        return null;

    }



    public static void GuardarPreferenciasLicencia(LicenciaDto licencia, string token, string? deviceId = null)

    {

        Preferences.Set(PreferenceKeys.LicenciaActivada, true);

        Preferences.Set(PreferenceKeys.LicenciaToken, token);

        Preferences.Set(PreferenceKeys.FechaVencimiento, licencia.FechaVencimiento);

        Preferences.Set(PreferenceKeys.NombreLocal, licencia.NombreLocal);

        Preferences.Set(PreferenceKeys.Dueno, licencia.Dueno);

        Preferences.Set(PreferenceKeys.IdNegocio, licencia.IdNegocio);

        if (!string.IsNullOrWhiteSpace(deviceId))

            LicenciaLocalStore.Guardar(token, licencia, deviceId);

    }

    /// <summary>Restaura licencia desde archivo local o revalida con token guardado al iniciar la app.</summary>
    public async Task RestaurarLicenciaAlInicioAsync()
    {
        if (Preferences.Get(PreferenceKeys.LicenciaActivada, false) &&
            !LicenciaHelper.EstaVencida())
            return;

        var local = LicenciaLocalStore.Cargar();
        if (local != null && LicenciaLocalStore.EsValidaLocalmente(local))
        {
            LicenciaLocalStore.AplicarAPreferencias(local);
            return;
        }

        var token = Preferences.Get(PreferenceKeys.LicenciaToken, local?.Token ?? string.Empty);
        if (string.IsNullOrWhiteSpace(token))
            return;

        if (local != null && LicenciaLocalStore.EsValidaLocalmente(local) &&
            string.Equals(local.Token, token, StringComparison.OrdinalIgnoreCase))
        {
            LicenciaLocalStore.AplicarAPreferencias(local);
            return;
        }

        var (resultado, licencia) = await ActivarAsync(token);
        if (resultado == ResultadoActivacionLicencia.Exito && licencia != null)
            GuardarPreferenciasLicencia(licencia, token, ObtenerDeviceId());
    }

    public void PersistirLicencia(LicenciaDto licencia, string token) =>
        GuardarPreferenciasLicencia(licencia, token, ObtenerDeviceId());



    public static string MensajeActivacion(ResultadoActivacionLicencia resultado) => resultado switch

    {

        ResultadoActivacionLicencia.SinConexion =>

            "Se requiere internet para activar. Verifica tu conexión e intenta de nuevo.",

        ResultadoActivacionLicencia.TokenInvalido =>

            "Token no válido. Verifica que lo hayas copiado correctamente.",

        ResultadoActivacionLicencia.DispositivoNoRegistrado =>

            "Este equipo no está registrado con ese token. Copia el Device ID y contacta a soporte para migrar la licencia (cambio de PC).",

        ResultadoActivacionLicencia.LicenciaVencida =>

            "La licencia está vencida. Contacta a soporte para renovar.",

        ResultadoActivacionLicencia.LicenciaInactiva =>

            "La licencia está suspendida. Contacta a soporte.",

        _ => "Licencia activada correctamente."

    };



    private async Task<List<LicenciaDto>?> ObtenerLicenciasAsync()

    {

        try

        {

            using var request = new HttpRequestMessage(HttpMethod.Get, LicenciaConfig.Url);

            if (!string.IsNullOrWhiteSpace(LicenciaConfig.ApiToken))
            {
                var esquema = LicenciaConfig.Url.Contains("githubusercontent.com", StringComparison.OrdinalIgnoreCase)
                    ? "token"
                    : "Bearer";
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(esquema, LicenciaConfig.ApiToken);
            }



            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)

                return null;



            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<LicenciaDto>>(json);

        }

        catch (HttpRequestException)

        {

            return null;

        }

        catch (TaskCanceledException)

        {

            return null;

        }

    }



    private static HttpClient CreateHttpClient()

    {

        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("BarberApp/1.0");

        return client;

    }

}


