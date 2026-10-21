using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: таблиця Programmers (нащадок Employees у TPT).</summary>
public class ProgrammerModelConfiguration : IEntityTypeConfiguration<ProgrammerModel>
{
    public void Configure(EntityTypeBuilder<ProgrammerModel> builder)
    {
        builder.ToTable("Programmers");

        builder.Property(p => p.PrimaryLanguage).IsRequired().HasMaxLength(30);

        // enum зберігається як текст ("Senior"), а не число — так дані в БД зрозуміліші
        builder.Property(p => p.Grade).HasConversion<string>().HasMaxLength(20);

        // один-до-багатьох: програміст → призначені баги (необов'язковий зв'язок; при видаленні програміста AssigneeId = NULL)
        builder.HasMany(p => p.AssignedBugs)
            .WithOne(b => b.Assignee)
            .HasForeignKey(b => b.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
