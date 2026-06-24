using BarberApp.Models;

namespace BarberApp.Services;

public static class SessionService
{
    public static void RegistrarActividad() =>
        Preferences.Set(PreferenceKeys.UltimaActividad, DateTime.UtcNow.Ticks);

    public static bool SesionExpirada()
    {
        var ticks = Preferences.Get(PreferenceKeys.UltimaActividad, 0L);
        if (ticks == 0) return false;

        var timeout = Preferences.Get(PreferenceKeys.TimeoutMinutos, 30);
        if (timeout <= 0) return false;

        var ultima = new DateTime(ticks, DateTimeKind.Utc);
        return DateTime.UtcNow - ultima > TimeSpan.FromMinutes(timeout);
    }

    public static void CerrarSesion()
    {
        Preferences.Remove(PreferenceKeys.UsuarioLogueadoId);
        Preferences.Remove(PreferenceKeys.UsuarioLogueadoNombre);
        Preferences.Remove(PreferenceKeys.UsuarioLogueadoRol);
        Preferences.Remove(PreferenceKeys.UltimaActividad);
    }

    public static void IniciarSesion(Barbero barbero)
    {
        Preferences.Set(PreferenceKeys.UsuarioLogueadoId, barbero.Id);
        Preferences.Set(PreferenceKeys.UsuarioLogueadoNombre, barbero.Nombre);
        Preferences.Set(PreferenceKeys.UsuarioLogueadoRol, barbero.Rol);
        RegistrarActividad();
    }
}
