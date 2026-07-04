namespace ArnoldOne.Server.Services;

public sealed class ErrorNotificationOptions
{
    public const string SectionName = "ErrorNotifications";

    public bool Enabled { get; set; }
    public string[] To { get; set; } = [];
    public int ThrottleMinutes { get; set; } = 10;
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
