namespace BarberApp.Core;

public static class LicenciaDateHelper
{
    public const int DiasAvisoVencimiento = 5;

    public static bool EstaVencida(string fechaVencimientoIso, DateTime hoy)
    {
        return DateTime.TryParse(fechaVencimientoIso, out var fecha) && hoy.Date > fecha.Date;
    }

    public static int? DiasRestantes(string fechaVencimientoIso, DateTime hoy)
    {
        if (!DateTime.TryParse(fechaVencimientoIso, out var fecha))
            return null;

        return (fecha.Date - hoy.Date).Days;
    }

    public static bool DebeMostrarAviso(string fechaVencimientoIso, DateTime hoy)
    {
        return DiasRestantes(fechaVencimientoIso, hoy) is int dias && dias >= 0 && dias <= DiasAvisoVencimiento;
    }

    public static bool EsFechaValida(string fechaVencimientoIso, DateTime hoy) =>
        DateTime.TryParse(fechaVencimientoIso, out var fecha) && fecha.Date >= hoy.Date;
}
