using System.Security.Cryptography;
using System.Text;

namespace BarberApp.Core;

public static class DeviceIdHelper
{
    /// <summary>Genera un Device ID estable de 16 caracteres hex a partir de una semilla de plataforma + instalación.</summary>
    public static string GenerarId(string semillaPlataforma, string idInstalacion)
    {
        var raw = $"{semillaPlataforma.Trim()}|{idInstalacion.Trim()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return string.Concat(bytes.Take(8).Select(b => b.ToString("X2")));
    }
}
