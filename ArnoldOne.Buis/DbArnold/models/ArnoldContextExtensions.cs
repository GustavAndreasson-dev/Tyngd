using Microsoft.EntityFrameworkCore;

namespace DbArnold.DbArnold.models;

public partial class ArnoldContext
{
    public virtual DbSet<UserSettings> UserSettings { get; set; }

    public virtual DbSet<ExerciseCatalog> ExerciseCatalog { get; set; }

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

        modelBuilder.Entity<ExerciseCatalog>(entity =>
        {
            entity.HasKey(e => e.CatalogExerciseId);
            entity.HasIndex(e => e.WgerUuid, "UQ_ExerciseCatalog_WgerUuid").IsUnique();

            entity.Property(e => e.NameEn).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NameSv).HasMaxLength(100);
            entity.Property(e => e.SourceHash).IsRequired().HasMaxLength(64).IsFixedLength().IsUnicode(false);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Muscles).IsRequired().HasMaxLength(400);
            entity.Property(e => e.Equipment).IsRequired().HasMaxLength(400);
            entity.Property(e => e.Aliases).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(400);
            entity.Property(e => e.License).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LicenseAuthor).IsRequired().HasMaxLength(400);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Exercise>()
            .HasOne(e => e.CatalogExercise).WithMany()
            .HasForeignKey(e => e.CatalogExerciseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Exercise_ExerciseCatalog");

        modelBuilder.Entity<ExerciseTemplate>()
            .HasOne(e => e.CatalogExercise).WithMany()
            .HasForeignKey(e => e.CatalogExerciseId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_ExerciseTemplate_ExerciseCatalog");
    }
}
