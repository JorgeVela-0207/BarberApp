namespace BarberApp.Helpers;

public static class ShareHelper
{
    public static async Task CompartirCitaWhatsAppAsync(CitaInfo cita)
    {
        if (string.IsNullOrWhiteSpace(cita.Telefono))
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = "Cita BarberApp",
                Text = cita.Mensaje
            });
            return;
        }

        var digits = new string(cita.Telefono.Where(char.IsDigit).ToArray());
        if (digits.Length >= 10)
        {
            var mensaje = Uri.EscapeDataString(cita.Mensaje);
            var url = $"https://wa.me/52{digits[^10..]}?text={mensaje}";
            await Launcher.Default.OpenAsync(url);
        }
        else
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = "Cita BarberApp",
                Text = cita.Mensaje
            });
        }
    }

    public record CitaInfo(string Telefono, string Mensaje);
}
