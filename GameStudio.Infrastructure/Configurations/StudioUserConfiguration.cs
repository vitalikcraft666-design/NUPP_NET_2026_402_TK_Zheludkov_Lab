using GameStudio.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: додаткові стовпці користувача в таблиці AspNetUsers (решту налаштовує IdentityDbContext).</summary>
public class StudioUserConfiguration : IEntityTypeConfiguration<StudioUser>
{
    public void Configure(EntityTypeBuilder<StudioUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(100);
    }
}
