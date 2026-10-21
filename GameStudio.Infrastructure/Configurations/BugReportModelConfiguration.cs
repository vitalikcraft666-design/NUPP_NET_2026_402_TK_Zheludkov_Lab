using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: таблиця BugReports (зв'язки з GameProjects і Programmers описані з боку «один»).</summary>
public class BugReportModelConfiguration : IEntityTypeConfiguration<BugReportModel>
{
    public void Configure(EntityTypeBuilder<BugReportModel> builder)
    {
        builder.ToTable("BugReports");

        builder.HasKey(b => b.Id);
        builder.HasAlternateKey(b => b.PublicId);

        builder.Property(b => b.Title).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Severity).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(b => new { b.Severity, b.IsFixed });

        builder.Navigation(b => b.GameProject).AutoInclude();
        builder.Navigation(b => b.Assignee).AutoInclude();
    }
}
