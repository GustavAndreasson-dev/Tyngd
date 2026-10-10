namespace ArnoldOne.Server.Services;

public sealed class ErrorNotificationOptions
{
    public const string SectionName = "ErrorNotifications";

    public bool Enabled { get; set; }
    public string[] To { get; set; } = [];
    public int ThrottleMinutes { get; set; } = 10;
    /// <summary>Hard cap on emails per hour across all errors; stops floods of unique messages. 0 = no cap.</summary>
    public int MaxPerHour { get; set; } = 20;
    public SmtpOptions Smtp { get; set; } = new();

    public sealed class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string From { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
