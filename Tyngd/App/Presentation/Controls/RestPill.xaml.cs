using Microsoft.UI.Dispatching;

namespace Tyngd.App.Presentation.Controls;

/// <summary>
/// Visar pågående vila (och "Vila klar" när den tagit slut). Sätt <see cref="Timer"/>, t.ex. via bindning till en vymodell.
/// </summary>
public sealed partial class RestPill : UserControl
{
    public static readonly DependencyProperty TimerProperty = DependencyProperty.Register(
        nameof(Timer), typeof(RestTimerState), typeof(RestPill),
        new PropertyMetadata(null, (d, e) => ((RestPill)d).OnTimerChanged((RestTimerState?)e.OldValue)));

    private DispatcherQueue? _dispatcher;
    private bool _finished;

    public RestPill()
    {
        this.InitializeComponent();
    }

    public RestTimerState? Timer
    {
        get => (RestTimerState?)GetValue(TimerProperty);
        set => SetValue(TimerProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Subscribe(Timer);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Unsubscribe(Timer);

    private void OnTimerChanged(RestTimerState? old)
    {
        Unsubscribe(old);
        if (IsLoaded)
            Subscribe(Timer);
    }

    private void Subscribe(RestTimerState? timer)
    {
        if (timer is null)
            return;
        Unsubscribe(timer);
        timer.OnTick += OnTick;
        timer.Finished += OnFinished;
        Update();
    }

    private void Unsubscribe(RestTimerState? timer)
    {
        if (timer is null)
            return;
        timer.OnTick -= OnTick;
        timer.Finished -= OnFinished;
    }

    private void OnTick() => _dispatcher?.TryEnqueue(Update);

    private void OnFinished() => _dispatcher?.TryEnqueue(() =>
    {
        _finished = true;
        Update();
    });

    private void Update()
    {
        var timer = Timer;
        if (timer?.IsResting == true)
            _finished = false;

        var remaining = timer?.RemainingSeconds ?? 0;
        TimeText.Text = _finished ? "Vila klar" : $"{remaining / 60}:{remaining % 60:00}";
        Visibility = timer is not null && (timer.IsResting || _finished) && timer.PassId is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void OnClick(object sender, RoutedEventArgs e)
    {
        if (Timer is not { SplitId: { } splitId, PassId: { } passId })
            return;
        _finished = false;
        Update();
        await this.Navigator()!.NavigateViewModelAsync<PassViewModel>(this, data: new PassArgs(splitId, passId));
    }
}
