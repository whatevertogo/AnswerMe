using AnswerMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Infrastructure.Data;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class AnswerMeDbContext : DbContext
{
    public AnswerMeDbContext(DbContextOptions<AnswerMeDbContext> options)
        : base(options)
    {
    }

    // DbSets - 使用 required 修饰符确保非空，由 EF Core 在运行时初始化
    public required DbSet<User> Users { get; set; }
    public required DbSet<DataSource> DataSources { get; set; }
    public required DbSet<QuestionBank> QuestionBanks { get; set; }
    public required DbSet<Question> Questions { get; set; }
    public required DbSet<Attempt> Attempts { get; set; }
    public required DbSet<AttemptDetail> AttemptDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        });

        // DataSource配置
        modelBuilder.Entity<DataSource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Config).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.DataSources)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuestionBank配置
        modelBuilder.Entity<QuestionBank>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DataSourceId);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Tags).HasDefaultValue("[]");

            entity.HasOne(e => e.User)
                .WithMany(u => u.QuestionBanks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DataSource)
                .WithMany(d => d.QuestionBanks)
                .HasForeignKey(e => e.DataSourceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Question配置
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.QuestionBankId);
            entity.HasIndex(e => e.QuestionType);
            entity.Property(e => e.QuestionText).IsRequired();
            entity.Property(e => e.QuestionType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CorrectAnswer).IsRequired();
            entity.Property(e => e.Difficulty).HasMaxLength(10).HasDefaultValue("medium");
            entity.Property(e => e.OrderIndex).IsRequired();

            entity.HasOne(e => e.QuestionBank)
                .WithMany(qb => qb.Questions)
                .HasForeignKey(e => e.QuestionBankId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Attempt配置
        modelBuilder.Entity<Attempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.QuestionBankId);
            entity.Property(e => e.Score).HasColumnType("NUMERIC(5,2)");
            entity.Property(e => e.TotalQuestions).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Attempts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.QuestionBank)
                .WithMany(qb => qb.Attempts)
                .HasForeignKey(e => e.QuestionBankId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AttemptDetail配置
        modelBuilder.Entity<AttemptDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AttemptId);
            entity.HasIndex(e => e.QuestionId);

            entity.HasOne(e => e.Attempt)
                .WithMany(a => a.AttemptDetails)
                .HasForeignKey(e => e.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.AttemptDetails)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
