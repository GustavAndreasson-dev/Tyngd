namespace Tyngd.App.Services;

/// <summary>
/// Vilan som systemnotis när appen ligger i bakgrunden: nedräkning medan vilan pågår och larm när den är slut
/// (som webbappens notis via service worker). I förgrunden sköter appen larmet själv. Bara Android; no-op annars.
/// </summary>
public static partial class RestNotifications
{
    /// <summary>Appen hamnade i bakgrunden: visa nedräkningen och schemalägg larmet om en vila pågår.</summary>
    public static void AppHidden(RestTimerState timer) => OnHidden(timer);

    /// <summary>Appen syns igen: ta bort notiserna och larmet.</summary>
    public static void AppShown()
    {
        OnShown();
        // Användaren kan ha ändrat tillståndet i telefonens inställningar under tiden.
        PermissionChanged?.Invoke();
    }

    /// <summary>Fråga om tillstånd att visa notiser (Android 13+), en gång per körning.</summary>
    public static void RequestPermission() => OnRequestPermission();

    /// <summary>Notiser finns på plattformen (inte desktop).</summary>
    public static bool IsSupported
    {
        get
        {
            var supported = false;
            CheckSupported(ref supported);
            return supported;
        }
    }

    /// <summary>Appen får visa notiser.</summary>
    public static bool AreAllowed
    {
        get
        {
            var allowed = false;
            CheckAllowed(ref allowed);
            return allowed;
        }
    }

    /// <summary>Utlöses när tillståndet kan ha ändrats (appen visas igen).</summary>
    public static event Action? PermissionChanged;

    /// <summary>Fråga om tillstånd, eller öppna appens notisinställningar om Android inte frågar igen.</summary>
    public static void AskOrOpenSettings() => OnAskOrOpenSettings();

    static partial void OnHidden(RestTimerState timer);
    static partial void OnShown();
    static partial void OnRequestPermission();
    static partial void CheckSupported(ref bool supported);
    static partial void CheckAllowed(ref bool allowed);
    static partial void OnAskOrOpenSettings();
}
