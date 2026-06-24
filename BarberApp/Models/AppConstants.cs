namespace BarberApp.Models;



public static class Roles

{

    public const string Admin = "admin";

    public const string Barbero = "barbero";

}



public static class MetodosPago

{

    public const string Efectivo = "Efectivo";

    public const string Tarjeta = "Tarjeta";

    public const string Transferencia = "Transferencia";



    public static readonly string[] Todos = [Efectivo, Tarjeta, Transferencia];

}



public static class RecurrenciaTipos

{

    public const string Ninguna = "ninguna";

    public const string Quincenal = "quincenal";

    public const string Mensual = "mensual";



    public static readonly string[] Opciones = [Ninguna, Quincenal, Mensual];

}



public static class PreferenceKeys

{

    public const string LicenciaActivada = "licencia_activada";

    public const string LicenciaToken = "licencia_token";

    public const string FechaVencimiento = "fecha_vencimiento";

    public const string NombreLocal = "nombre_local";

    public const string Dueno = "dueno";

    public const string TelefonoNegocio = "telefono_negocio";

    public const string HoraApertura = "hora_apertura";

    public const string HoraCierre = "hora_cierre";

    public const string IdNegocio = "id_negocio";

    public const string SucursalActivaId = "sucursal_activa_id";

    public const string UsuarioLogueadoId = "usuario_logueado_id";

    public const string UsuarioLogueadoNombre = "usuario_logueado_nombre";

    public const string UsuarioLogueadoRol = "usuario_logueado_rol";

    public const string TemaOscuro = "tema_oscuro";

    public const string UltimaActividad = "ultima_actividad";

    public const string TimeoutMinutos = "timeout_minutos";

    public const string UltimoRespaldo = "ultimo_respaldo";

    public const string SyncUrl = "sync_url";

    public const string SyncUltima = "sync_ultima";

    public const string DeviceInstallId = "device_install_id";

}


