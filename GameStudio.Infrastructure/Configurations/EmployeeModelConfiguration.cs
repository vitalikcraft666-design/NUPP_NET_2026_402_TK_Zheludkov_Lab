using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStudio.Infrastructure.Configurations;

/// <summary>Fluent API: базова таблиця ієрархії працівників (Table-per-Type).</summary>
public class EmployeeModelConfiguration : IEntityTypeConfiguration<EmployeeModel>
{
    public void Configure(EntityTypeBuilder<EmployeeModel> builder)
    {
        builder.ToTable("Employees");

        // наслідування «таблиця на тип»: Employees + Programmers + Artists, зв'язані первинним ключем
        builder.UseTptMappingStrategy();

        builder.HasKey(e => e.Id);

        // альтернативний ключ: унікальний Guid доменного об'єкта
        builder.HasAlternateKey(e => e.PublicId);

        builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);

        // SQLite не має типу decimal, тому гроші зберігаємо як REAL (double), щоб працювали SUM/AVG/ORDER BY у БД
        builder.Property(e => e.BaseSalary).HasConversion<double>();

        builder.HasIndex(e => e.FullName);
    }
}
