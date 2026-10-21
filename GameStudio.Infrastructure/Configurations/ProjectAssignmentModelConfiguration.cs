using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: додаткові налаштування проміжної таблиці ProjectAssignments.</summary>
public class ProjectAssignmentModelConfiguration : IEntityTypeConfiguration<ProjectAssignmentModel>
{
    public void Configure(EntityTypeBuilder<ProjectAssignmentModel> builder)
    {
        builder.Navigation(a => a.Employee).AutoInclude();
    }
}
