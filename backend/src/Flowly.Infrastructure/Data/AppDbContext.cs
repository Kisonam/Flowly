using System;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Notes
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<NoteTag> NoteTags { get; set; } = null!;

    // Tasks
    public DbSet<TaskItem> Tasks { get; set; } = null!;
    public DbSet<TaskTheme> TaskThemes { get; set; } = null!;
    public DbSet<TaskSubtask> TaskSubtasks { get; set; } = null!;
    public DbSet<TaskRecurrence> TaskRecurrences { get; set; } = null!;
    public DbSet<TaskTag> TaskTags { get; set; } = null!;

    // Finance
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;
    public DbSet<FinancialGoal> FinancialGoals { get; set; } = null!;

    // Shared
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<MediaAsset> MediaAssets { get; set; } = null!;
    public DbSet<Link> Links { get; set; } = null!;
    public DbSet<ArchiveEntry> ArchiveEntries { get; set; } = null!;

    // ============================================
    // Configuration
    // ============================================

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Custom table names for Identity (optional - cleaner naming)
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
        });

        builder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        // Seed data
        SeedData(builder);
    }
    private void SeedData(ModelBuilder builder)
    {
        builder.Entity<Currency>().HasData(
            new Currency { Code = "USD", Name = "US Dollar", Symbol = "$" },
            new Currency { Code = "EUR", Name = "Euro", Symbol = "€" },
            new Currency { Code = "UAH", Name = "Ukrainian Hryvnia", Symbol = "₴" },
            new Currency { Code = "PLN", Name = "Polish Zloty", Symbol = "zł" }
        );
        var defaultCategories = new[]
        {
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Food & Drinks" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Transport" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Shopping" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Entertainment" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Health" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Education" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Utilities" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Salary" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Freelance" },
            new Category { Id = Guid.NewGuid(), UserId = null, Name = "Other" }
        };

        builder.Entity<Category>().HasData(defaultCategories);
    }
}
