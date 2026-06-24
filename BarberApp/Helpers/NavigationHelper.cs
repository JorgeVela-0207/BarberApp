namespace BarberApp.Helpers;

public static class NavigationHelper
{
    public static void SetRootPage(Page page)
    {
        var nav = new NavigationPage(page);
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = nav;
    }

    public static NavigationPage? GetNavigationPage()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        return window?.Page as NavigationPage;
    }
}
