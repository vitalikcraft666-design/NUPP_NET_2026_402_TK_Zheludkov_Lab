using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: таблиця Artists (нащадок Employees у TPT).</summary>
public class ArtistModelConfiguration : IEntityTypeConfiguration<ArtistModel>
{
    public void Configure(EntityTypeBuilder<ArtistModel> builder)
    {
        builder.ToTable("Artists");

        builder.Property(a => a.Specialization).HasConversion<string>().HasMaxLength(30);

        builder.Property(a => a.MainTool).IsRequired().HasMaxLength(50);
    }
}
