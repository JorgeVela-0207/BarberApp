namespace BarberApp.Core;

public static class CitaHorarioHelper
{
    public static bool ParseHora(string hora, out TimeSpan inicio)
    {
        inicio = default;
        if (TimeSpan.TryParse(hora, out inicio))
            return true;

        if (DateTime.TryParse(hora, out var dt))
        {
            inicio = dt.TimeOfDay;
            return true;
        }

        return false;
    }

    public static bool HaySolapamiento(
        TimeSpan inicioA, int duracionA,
        TimeSpan inicioB, int duracionB)
    {
        var finA = inicioA.Add(TimeSpan.FromMinutes(duracionA));
        var finB = inicioB.Add(TimeSpan.FromMinutes(duracionB));
        return inicioA < finB && inicioB < finA;
    }

    public static bool EstaEnRango(TimeSpan hora, TimeSpan inicio, TimeSpan fin) =>
        hora >= inicio && hora < fin;

    public static string FormatearHora(TimeSpan hora) =>
        hora.ToString(@"hh\:mm");
}
