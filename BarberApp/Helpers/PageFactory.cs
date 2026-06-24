namespace BarberApp.Helpers;

public static class PageFactory
{
    private static IServiceProvider Sp =>
        Application.Current!.Handler!.MauiContext!.Services;

    public static T Get<T>() where T : notnull =>
        Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<T>(Sp);

    public static Views.DashboardPage Dashboard() => Get<Views.DashboardPage>();
    public static Views.AgendaPage Agenda() => Get<Views.AgendaPage>();
    public static Views.CajaPage Caja() => Get<Views.CajaPage>();
    public static Views.CatalogosPage Catalogos() => Get<Views.CatalogosPage>();
    public static Views.ConfiguracionPage Configuracion() => Get<Views.ConfiguracionPage>();
    public static Views.LoginPage Login() => Get<Views.LoginPage>();
}
