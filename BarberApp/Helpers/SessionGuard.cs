using BarberApp.Helpers;
using BarberApp.Models;
using BarberApp.Services;
using BarberApp.Views;

namespace BarberApp;

public static class SessionGuard
{
    public static async Task<bool> VerificarAsync(ContentPage page)
    {
        ThemeService.AplicarAPagina(page);

        if (!SessionService.SesionExpirada())
        {
            SessionService.RegistrarActividad();
            return true;
        }

        SessionService.CerrarSesion();
        if (Application.Current?.Handler?.MauiContext != null)
            NavigationHelper.SetRootPage(PageFactory.Login());
        else
            await page.DisplayAlert("Sesión", "Tu sesión expiró. Vuelve a iniciar sesión.", "OK");
        return false;
    }
}
