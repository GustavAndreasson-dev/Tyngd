using Microsoft.EntityFrameworkCore;

namespace DbArnold.DbArnold.models;

public partial class ArnoldContext
{
    public virtual DbSet<UserSettings> UserSettings { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.IsDarkMode).HasDefaultValue(false);
            entity.Property(e => e.IsRestTimerEnabled).HasDefaultValue(true);
            entity.Property(e => e.RestTimerSeconds).HasDefaultValue(120);

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<UserSettings>(e => e.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSettings_UserInfo");
        });
    }
}
