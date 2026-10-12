#nullable disable
using System;

namespace Tyngd.Data.Models;

public partial class UserSettings
{
    public Guid UserId { get; set; }

    public bool IsDarkMode { get; set; }

    public bool IsRestTimerEnabled { get; set; }

    public int RestTimerSeconds { get; set; }

    public virtual UserInfo User { get; set; }
}
