
namespace ArnoldOne.Shared;

/// <summary>
/// Applikationens huvudlayout med navigeringslåda (drawer).
/// </summary>
public partial class MainLayout
{
    /// <summary>Styr om navigeringslådan är öppen eller stängd.</summary>
    bool _drawerOpen = true;

    /// <summary>Växlar navigeringslådans öppna/stängda tillstånd.</summary>
    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}
