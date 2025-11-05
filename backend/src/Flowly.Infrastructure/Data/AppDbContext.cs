using System;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    // Notes
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<NoteGroup> NoteGroups { get; set; } = null!;
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
    
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    // ============================================
    // Configuration
    // ============================================

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Currency>(entity =>
                {
                    entity.HasKey(e => e.Code);
                    entity.Property(e => e.Code).HasMaxLength(3);
                    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                    entity.Property(e => e.Symbol).HasMaxLength(10).IsRequired();
                });

        // Category
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique(); // Unique per user
        });

        builder.Entity<NoteTag>(entity =>
        {
            entity.HasKey(e => new { e.NoteId, e.TagId }); // Composite Key
        });

        // NoteGroup relationship handled via configurations

        // TaskTag - Many-to-Many join table
        builder.Entity<TaskTag>(entity =>
        {
            entity.HasKey(e => new { e.TaskId, e.TagId }); // Composite Key
        });

        // Apply all configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

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
            entity.HasKey(e => new { e.UserId, e.RoleId });
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
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
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), UserId = null, Name = "Food & Drinks" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), UserId = null, Name = "Transport" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), UserId = null, Name = "Shopping" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), UserId = null, Name = "Entertainment" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), UserId = null, Name = "Health" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), UserId = null, Name = "Education" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), UserId = null, Name = "Utilities" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), UserId = null, Name = "Salary" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), UserId = null, Name = "Freelance" },
            new Category { Id = Guid.Parse("00000000-0000-0000-0000-00000000000A"), UserId = null, Name = "Other" }
        };

        builder.Entity<Category>().HasData(defaultCategories);
    }

    // ============================================
    // Save Changes Override (for automatic timestamps)
    // ============================================

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            // Update CreatedAt for new entities
            if (entry.State == EntityState.Added)
            {
                switch (entry.Entity)
                {
                    case ApplicationUser user:
                        user.CreatedAt = now;
                        break;
                    case Note note:
                        note.CreatedAt = now;
                        note.UpdatedAt = now;
                        break;
                    case TaskItem task:
                        task.CreatedAt = now;
                        task.UpdatedAt = now;
                        break;
                    case Transaction transaction:
                        transaction.CreatedAt = now;
                        transaction.UpdatedAt = now;
                        break;
                    case FinancialGoal goal:
                        goal.CreatedAt = now;
                        goal.UpdatedAt = now;
                        break;
                }
            }

            // Update UpdatedAt for modified entities
            if (entry.State == EntityState.Modified)
            {
                switch (entry.Entity)
                {
                    case Note note:
                        note.UpdatedAt = now;
                        break;
                    case TaskItem task:
                        task.UpdatedAt = now;
                        break;
                    case Transaction transaction:
                        transaction.UpdatedAt = now;
                        break;
                    case FinancialGoal goal:
                        goal.UpdatedAt = now;
                        break;
                }
            }
        }
    }
}
