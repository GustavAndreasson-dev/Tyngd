using Windows.UI.Core;

namespace Tyngd.App.Presentation;

/// <summary>
/// Androids bakåtknapp. Från Android 16 ignoreras <see cref="BackRequestedEventArgs.Handled"/>: så länge appen
/// prenumererar konsumeras trycket. Prenumerera därför bara medan sidan kan gå bakåt, annars stängs inte appen.
/// </summary>
public static class BackButton
{
    /// <summary>Helskärmssidor (Pass, Redigera split, Övning): bakåt går tillbaka medan sidan visas.</summary>
    public static void GoBackOnBackButton(this Page page) =>
        page.HandleBackButton(() => _ = page.Navigator()?.NavigateBackAsync(page));

    /// <summary>Kör <paramref name="onBack"/> på bakåt medan elementet är laddat.</summary>
    public static void HandleBackButton(this FrameworkElement element, Action onBack)
    {
        var subscribed = false;
        void OnBackRequested(object? sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            onBack();
        }

        element.Loaded += (_, _) =>
        {
            if (subscribed)
                return;
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            subscribed = true;
        };
        element.Unloaded += (_, _) =>
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
            subscribed = false;
        };
    }
}
