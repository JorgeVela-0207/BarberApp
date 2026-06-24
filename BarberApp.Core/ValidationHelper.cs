namespace BarberApp.Core;

public static class ValidationHelper
{
    public static bool EsTelefonoValido(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return true;

        var digits = new string(telefono.Where(char.IsDigit).ToArray());
        return digits.Length is >= 10 and <= 15;
    }

    public static string NormalizarTelefono(string telefono)
    {
        var digits = new string(telefono.Where(char.IsDigit).ToArray());
        return digits.Length > 10 ? digits[^10..] : digits;
    }

    public static bool EsPrecioValido(string texto, out decimal precio) =>
        decimal.TryParse(texto, out precio) && precio >= 0;

    public static bool EsPorcentajeValido(string texto, out decimal porcentaje) =>
        decimal.TryParse(texto, out porcentaje) && porcentaje is >= 0 and <= 100;
}
