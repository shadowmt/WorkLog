
using Microsoft.EntityFrameworkCore;
using WorkLog.Domain.Entities;

namespace WorkLog.Infrastructure.Persistence;

public sealed class WorkLogDbContext(DbContextOptions<WorkLogDbContext> options)
    : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<WorkLogEntry> WorkLogEntries => Set<WorkLogEntry>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureProject(modelBuilder);
        ConfigureWorkLogEntry(modelBuilder);
    }

    private static void ConfigureProject(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Project>();

        // EF Core でのテーブル名を指定
        entity.ToTable("projects");

        // 主キーを指定
        entity.HasKey(x => x.Id);

        // Name は最大 100 文字まで許容し、必須項目とする
        entity.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        // Description は最大 1000 文字まで許容する
        entity.Property(x => x.Description)
            .HasMaxLength(1000);

        entity.Property(x => x.IsActive)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        // Trim 後の大文字・小文字を区別しない一意制約
        entity.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ux_projects_name_normalized")
            .HasAnnotation("Npgsql:IndexMethod", "btree")
            .HasFilter(null);
    }

    private static void ConfigureWorkLogEntry(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<WorkLogEntry>();

        // EF Core でのテーブル名を指定
        entity.ToTable("work_log_entries");

        // 主キーを指定
        entity.HasKey(x => x.Id);

        entity.Property(x => x.WorkDate)
            .IsRequired();

        entity.Property(x => x.ProjectId)
            .IsRequired();

        // Description は最大 2000 文字まで許容する
        entity.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        entity.Property(x => x.DurationMinutes)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.UpdatedAt)
            .IsRequired();

        // WorkLogEntry と Project のリレーションを設定。参照中のプロジェクトの削除を禁止
        entity.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // インデックスを設定
        entity.HasIndex(x => x.WorkDate)
            .HasDatabaseName("ix_work_log_entries_work_date");

        entity.HasIndex(x => x.ProjectId)
            .HasDatabaseName("ix_work_log_entries_project_id");
    }
}
