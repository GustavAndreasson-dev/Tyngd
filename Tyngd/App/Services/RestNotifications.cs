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
    public static void AppShown() => OnShown();

    /// <summary>Fråga om tillstånd att visa notiser (Android 13+), en gång per körning.</summary>
    public static void RequestPermission() => OnRequestPermission();

    static partial void OnHidden(RestTimerState timer);
    static partial void OnShown();
    static partial void OnRequestPermission();
}
