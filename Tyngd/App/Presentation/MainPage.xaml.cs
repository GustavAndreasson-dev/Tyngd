using Windows.UI.Core;

namespace Tyngd.App.Presentation;

public sealed partial class MainPage : Page
{
    private readonly Page[] _tabs;
    private readonly Type[] _tabViewModels =
        [typeof(OverviewViewModel), typeof(MySplitsViewModel), typeof(TemplatesViewModel), typeof(ExercisesViewModel), typeof(SettingsViewModel)];
    private readonly bool[] _created = new bool[5];

    public const int SplitsTabIndex = 1;

    // Flik som ska visas nästa gång skalet visas, t.ex. Mina splits efter att ett löst pass sparats.
    private static int? s_requestedTab;

    public static void RequestTab(int index) => s_requestedTab = index;

    public MainPage()
    {
        this.InitializeComponent();
        _tabs = [OverviewTab, SplitsTab, TemplatesTab, ExercisesTab, SettingsTab];
        Loaded += (_, _) =>
        {
            if (s_requestedTab is int requested)
            {
                s_requestedTab = null;
                Tabs.SelectedIndex = requested;
            }
            ShowTab(Math.Max(0, Tabs.SelectedIndex));
        };
        Unloaded += (_, _) => SetBackToOverview(false);
    }

    // Bakåt från en annan flik går till Översikt; på Översikt lämnas bakåt till Android (stänger appen).
    // Prenumerera bara när det behövs – se BackButton om Android 16.
    private bool _backSubscribed;

    private void SetBackToOverview(bool enabled)
    {
        if (enabled == _backSubscribed)
            return;
        if (enabled)
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
        else
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
        _backSubscribed = enabled;
    }

    private void OnBackRequested(object? sender, BackRequestedEventArgs e)
    {
        e.Handled = true;
        Tabs.SelectedIndex = 0;
    }

    // Flikarna växlas här i stället för med Uno-navigeringens regioner: TabBar-navigatorn hittade inte flikarnas
    // sidor ("has no children to forward request to") och visade fel eller tomt innehåll på telefonen.
    private void OnTabChanged(TabBar sender, TabBarSelectionChangedEventArgs args) => ShowTab(sender.SelectedIndex);

    private void ShowTab(int index)
    {
        if (index < 0 || index >= _tabs.Length)
            return;

        if (!_created[index])
        {
            // Skalets navigator till de flikar som tar en, så att de kan öppna helskärmssidorna (Pass, EditSplit, ExerciseInfo).
            var type = _tabViewModels[index];
            object[] args = type.GetConstructors()[0].GetParameters().Any(p => p.ParameterType == typeof(INavigator))
                ? [this.Navigator()!]
                : [];
            _tabs[index].DataContext = ActivatorUtilities.CreateInstance(this.FindServiceProvider()!, type, args);
            _created[index] = true;
        }

        for (var i = 0; i < _tabs.Length; i++)
            _tabs[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;

        SetBackToOverview(IsLoaded && index != 0);
    }
}
