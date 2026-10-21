using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: таблиця StorePages.</summary>
public class StorePageModelConfiguration : IEntityTypeConfiguration<StorePageModel>
{
    public void Configure(EntityTypeBuilder<StorePageModel> builder)
    {
        builder.ToTable("StorePages");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Url).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Price).HasConversion<double>();

        // обмеження CHECK на рівні БД
        builder.ToTable(t => t.HasCheckConstraint("CK_StorePages_WishlistCount", "\"WishlistCount\" >= 0"));
    }
}
