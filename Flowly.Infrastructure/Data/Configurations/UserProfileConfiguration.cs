using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("UserProfiles");

            builder.HasIndex(x => x.UserId).IsUnique(); // 1:1 з користувачем

            builder.Property(x => x.UserId).HasMaxLength(256).IsRequired();
            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.PreferredCulture).HasMaxLength(12).IsRequired();
            builder.Property(x => x.AvatarPath).HasMaxLength(512);
        }
    }
}