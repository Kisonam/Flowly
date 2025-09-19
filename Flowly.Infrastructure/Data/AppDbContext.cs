using System;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Підхоплюємо окремі конфіги мапінгу (вже доданий UserProfileConfiguration).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Глобальний фільтр м’якого видалення для всіх BaseEntity-нащадків.
        // Чому так: щоб випадково не показувати "видалені" записи в усьому застосунку.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var prop = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var compare = System.Linq.Expressions.Expression.Equal(prop,
                            System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(compare, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Автоматична постановка CreatedAt/UpdatedAt — менше шаблонного коду в сервісах.
        var entries = ChangeTracker.Entries<BaseEntity>();
        var utcNow = DateTime.UtcNow;

        foreach (var e in entries)
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.CreatedAt = utcNow;
                    e.Entity.UpdatedAt = null;
                    break;
                case EntityState.Modified:
                    // Не чіпаємо CreatedAt, лише UpdatedAt.
                    e.Property(x => x.CreatedAt).IsModified = false;
                    e.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Deleted:
                    // М’яке видалення: перетворюємо Delete на Update з IsDeleted=true.
                    e.State = EntityState.Modified;
                    e.Entity.IsDeleted = true;
                    e.Entity.DeletedAt ??= utcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
