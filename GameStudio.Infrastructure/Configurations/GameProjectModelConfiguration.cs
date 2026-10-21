using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: таблиця GameProjects та всі її зв'язки.</summary>
public class GameProjectModelConfiguration : IEntityTypeConfiguration<GameProjectModel>
{
    public void Configure(EntityTypeBuilder<GameProjectModel> builder)
    {
        builder.ToTable("GameProjects");

        builder.HasKey(p => p.Id);
        builder.HasAlternateKey(p => p.PublicId);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Genre).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Engine).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Budget).HasConversion<double>();
        builder.Property(p => p.Spent).HasConversion<double>();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.Status);

        // один-до-одного: проєкт ↔ сторінка в магазині.
        // Зовнішній ключ GameProjectId — у залежній таблиці StorePages; EF створює для нього унікальний індекс
        builder.HasOne(p => p.StorePage)
            .WithOne(s => s.GameProject)
            .HasForeignKey<StorePageModel>(s => s.GameProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // один-до-багатьох: проєкт → баги (обов'язковий зв'язок; баги видаляються разом із проєктом)
        builder.HasMany(p => p.Bugs)
            .WithOne(b => b.GameProject)
            .HasForeignKey(b => b.GameProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // багато-до-багатьох: проєкти ↔ працівники через проміжну таблицю ProjectAssignments з додатковими полями
        builder.HasMany(p => p.TeamMembers)
            .WithMany(e => e.Projects)
            .UsingEntity<ProjectAssignmentModel>(
                right => right.HasOne(a => a.Employee)
                    .WithMany(e => e.Assignments)
                    .HasForeignKey(a => a.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne(a => a.GameProject)
                    .WithMany(p => p.Assignments)
                    .HasForeignKey(a => a.GameProjectId)
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("ProjectAssignments");
                    join.HasKey(a => new { a.GameProjectId, a.EmployeeId });
                    join.Property(a => a.Role).IsRequired().HasMaxLength(50);
                });

        // пов'язані дані, які потрібні для перетворення в доменну модель, завантажуються автоматично
        builder.Navigation(p => p.StorePage).AutoInclude();
        builder.Navigation(p => p.Assignments).AutoInclude();
    }
}
