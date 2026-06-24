using BarberApp.Core;

using BarberApp.Models;



namespace BarberApp.Helpers;



public static class LicenciaHelper

{

    public const int DiasAvisoVencimiento = LicenciaDateHelper.DiasAvisoVencimiento;



    public static bool EstaVencida()

    {

        var fechaStr = Preferences.Get(PreferenceKeys.FechaVencimiento, string.Empty);

        return LicenciaDateHelper.EstaVencida(fechaStr, DateTime.Today);

    }



    public static int? DiasRestantes()

    {

        var fechaStr = Preferences.Get(PreferenceKeys.FechaVencimiento, string.Empty);

        return LicenciaDateHelper.DiasRestantes(fechaStr, DateTime.Today);

    }



    public static bool DebeMostrarAviso() =>

        LicenciaDateHelper.DebeMostrarAviso(

            Preferences.Get(PreferenceKeys.FechaVencimiento, string.Empty),

            DateTime.Today);

}


