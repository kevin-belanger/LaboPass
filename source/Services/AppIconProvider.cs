namespace LaboPass.Services;

public static class AppIconProvider
{
    private static Icon? cachedIcon;

    public static Icon? GetApplicationIcon()
    {
        if (cachedIcon is not null)
        {
            return cachedIcon;
        }

        try
        {
            cachedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            cachedIcon = null;
        }

        return cachedIcon;
    }
}
