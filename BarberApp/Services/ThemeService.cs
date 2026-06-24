using BarberApp.Models;

namespace BarberApp.Services;

public static class ThemeService
{
    public static bool EsOscuro() =>
        Preferences.Get(PreferenceKeys.TemaOscuro, true);

    public static void AplicarTemaGuardado() => AplicarTema(EsOscuro());

    public static void AplicarTema(bool oscuro)
    {
        Preferences.Set(PreferenceKeys.TemaOscuro, oscuro);
        Application.Current!.UserAppTheme = oscuro ? AppTheme.Dark : AppTheme.Light;
        ActualizarRecursos(oscuro);
    }

    public static void AlternarTema() => AplicarTema(!EsOscuro());

    public static void ActualizarRecursos(bool? oscuro = null)
    {
        oscuro ??= EsOscuro();
        if (Application.Current?.Resources == null) return;

        var r = Application.Current.Resources;
        if (oscuro.Value)
        {
            Set(r, "PageBackground", "#0d0d10");
            Set(r, "CardBackground", "#16161a");
            Set(r, "CardMuted", "#12121a");
            Set(r, "CardBorder", "#2a2a32");
            Set(r, "Divider", "#2a2a32");
            Set(r, "TextPrimary", "#F5F3F0");
            Set(r, "TextSecondary", "#8A8A95");
            Set(r, "TextMuted", "#6a6a75");
            Set(r, "AccentGold", "#C4A77D");
            Set(r, "AccentGoldStat", "#D4A853");
            Set(r, "AccentGoldSoft", "#22C4A77D");
            Set(r, "AccentLavender", "#9B8EC4");
            Set(r, "AccentGreen", "#6DB897");
            Set(r, "InputBackground", "#12121a");
            Set(r, "ButtonOnAccent", "#1a1a1e");
            Set(r, "BannerWarning", "#2a2218");
            Set(r, "BannerWarningText", "#D4A853");
            Set(r, "GlowGold", "#18C4A77D");
            Set(r, "AccentGreenSoft", "#1a2a22");
            Set(r, "NavMutedText", "#555560");
        }
        else
        {
            Set(r, "PageBackground", "#f5f3f0");
            Set(r, "CardBackground", "#ffffff");
            Set(r, "CardMuted", "#f0eeea");
            Set(r, "CardBorder", "#ddd8cf");
            Set(r, "Divider", "#e8e3da");
            Set(r, "TextPrimary", "#1a1a1e");
            Set(r, "TextSecondary", "#5a5a65");
            Set(r, "TextMuted", "#8a8a95");
            Set(r, "AccentGold", "#9a7b4f");
            Set(r, "AccentGoldStat", "#8a6d3f");
            Set(r, "AccentGoldSoft", "#33C4A77D");
            Set(r, "AccentLavender", "#7a6ea8");
            Set(r, "AccentGreen", "#4a9a75");
            Set(r, "InputBackground", "#faf9f7");
            Set(r, "ButtonOnAccent", "#ffffff");
            Set(r, "BannerWarning", "#fff8ec");
            Set(r, "BannerWarningText", "#8a6d3f");
            Set(r, "GlowGold", "#22C4A77D");
            Set(r, "AccentGreenSoft", "#eef7f2");
            Set(r, "NavMutedText", "#8a8a95");
        }

        // Sincronizar colores MAUI base usados por Styles.xaml
        Set(r, "OffBlack", oscuro.Value ? "#0d0d10" : "#f5f3f0");
        Set(r, "Primary", oscuro.Value ? "#C4A77D" : "#9a7b4f");
        Set(r, "PrimaryDark", oscuro.Value ? "#C4A77D" : "#9a7b4f");
    }

    private static void Set(ResourceDictionary r, string key, string hex) =>
        r[key] = Color.FromArgb(hex);

    public static void AplicarAPagina(ContentPage page)
    {
        ActualizarRecursos();
        page.BackgroundColor = GetColor("PageBackground");
    }

    public static Color GetColor(string key) =>
        Application.Current?.Resources[key] as Color ?? Colors.Gray;

    public static string GetColorHex(string key)
    {
        var hex = GetColor(key).ToArgbHex();
        return hex.Length == 9 ? $"#{hex[3..]}" : hex;
    }
}
