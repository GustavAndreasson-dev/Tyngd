using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace Tyngd.App.Services;

public static partial class RestNotifications
{
    private const string CountdownChannelId = "tyngd-rest";
    private const string FinishedChannelId = "tyngd-rest-done";
    internal const int CountdownNotificationId = 1;
    internal const int FinishedNotificationId = 2;
    private static bool s_permissionRequested;

    private static Context AppContext => Android.App.Application.Context;

    static partial void OnRequestPermission()
    {
        if (s_permissionRequested || !OperatingSystem.IsAndroidVersionAtLeast(33))
            return;
        if (ContextCompat.CheckSelfPermission(AppContext, Manifest.Permission.PostNotifications) == Permission.Granted)
            return;
        if (Uno.UI.ContextHelper.Current is Activity activity)
        {
            s_permissionRequested = true;
            ActivityCompat.RequestPermissions(activity, [Manifest.Permission.PostNotifications], requestCode: 1);
        }
    }

    static partial void OnHidden(RestTimerState timer)
    {
        if (!timer.IsResting || timer.EndsAtUtc is not { } endsAt)
            return;

        var context = AppContext;
        EnsureChannels(context);
        var endMs = endsAt.ToUnixTimeMilliseconds();

        // Systemet räknar ner själv (kronometer), så inget behöver köras medan appen ligger i bakgrunden.
        var countdownBuilder = new NotificationCompat.Builder(context, CountdownChannelId);
        countdownBuilder.SetSmallIcon(Android.Resource.Drawable.IcLockIdleAlarm);
        countdownBuilder.SetContentTitle("Vila");
        countdownBuilder.SetContentText("Tryck för att gå tillbaka till passet.");
        countdownBuilder.SetWhen(endMs);
        countdownBuilder.SetUsesChronometer(true);
        countdownBuilder.SetChronometerCountDown(true);
        countdownBuilder.SetShowWhen(true);
        countdownBuilder.SetOngoing(true);
        countdownBuilder.SetOnlyAlertOnce(true);
        countdownBuilder.SetTimeoutAfter(Math.Max(1000, endMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        countdownBuilder.SetContentIntent(OpenAppIntent(context));
        var countdown = countdownBuilder.Build()!;
        NotificationManagerCompat.From(context)!.Notify(CountdownNotificationId, countdown);

        ScheduleAlarm(context, endMs);
    }

    static partial void OnShown()
    {
        var context = AppContext;
        if (context.GetSystemService(Context.AlarmService) is AlarmManager alarms)
            alarms.Cancel(AlarmIntent(context));
        var notifications = NotificationManagerCompat.From(context)!;
        notifications.Cancel(CountdownNotificationId);
        notifications.Cancel(FinishedNotificationId);
    }

    private static void ScheduleAlarm(Context context, long endMs)
    {
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarms)
            return;

        var intent = AlarmIntent(context);
        // Exakt larm om användaren tillåtit det (Android 12+ kräver tillstånd); annars så nära Android tillåter.
        if (!OperatingSystem.IsAndroidVersionAtLeast(31) || alarms.CanScheduleExactAlarms())
            alarms.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, endMs, intent);
        else
            alarms.SetAndAllowWhileIdle(AlarmType.RtcWakeup, endMs, intent);
    }

    /// <summary>Larmet: notis med ljud och vibration. Anropas från <see cref="RestAlarmReceiver"/>.</summary>
    internal static void ShowFinished(Context context)
    {
        EnsureChannels(context);
        var notifications = NotificationManagerCompat.From(context)!;
        notifications.Cancel(CountdownNotificationId);
        if (!notifications.AreNotificationsEnabled())
            return;

        var finishedBuilder = new NotificationCompat.Builder(context, FinishedChannelId);
        finishedBuilder.SetSmallIcon(Android.Resource.Drawable.IcLockIdleAlarm);
        finishedBuilder.SetContentTitle("Vilan är slut");
        finishedBuilder.SetContentText("Dags för nästa set.");
        finishedBuilder.SetPriority(NotificationCompat.PriorityHigh);
        finishedBuilder.SetCategory(NotificationCompat.CategoryAlarm);
        finishedBuilder.SetAutoCancel(true);
        finishedBuilder.SetContentIntent(OpenAppIntent(context));
        var finished = finishedBuilder.Build()!;
        notifications.Notify(FinishedNotificationId, finished);
    }

    private static void EnsureChannels(Context context)
    {
        if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            return;

        // Nedräkningen ska inte låta; larmet ska höras och vibrera.
        manager.CreateNotificationChannel(new NotificationChannel(CountdownChannelId, "Vila pågår", NotificationImportance.Low)
        {
            Description = "Nedräkning medan vilan pågår och appen ligger i bakgrunden.",
        });
        var done = new NotificationChannel(FinishedChannelId, "Vilan är slut", NotificationImportance.High)
        {
            Description = "Larm när vilan är slut och appen ligger i bakgrunden.",
        };
        done.EnableVibration(true);
        done.SetVibrationPattern([0, 250, 120, 250]);
        manager.CreateNotificationChannel(done);
    }

    private static PendingIntent OpenAppIntent(Context context)
    {
        var intent = context.PackageManager!.GetLaunchIntentForPackage(context.PackageName!)!;
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        return PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
    }

    private static PendingIntent AlarmIntent(Context context) =>
        PendingIntent.GetBroadcast(context, 0, new Intent(context, typeof(RestAlarmReceiver)),
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
}

/// <summary>Tar emot larmet när vilan är slut, även om appen inte körs.</summary>
[BroadcastReceiver(Exported = false)]
public class RestAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is not null)
            RestNotifications.ShowFinished(context);
    }
}
