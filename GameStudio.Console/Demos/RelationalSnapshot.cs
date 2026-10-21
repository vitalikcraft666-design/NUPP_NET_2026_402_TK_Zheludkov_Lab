using GameStudio.Infrastructure;
using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.ConsoleApp.Demos;

/// <summary>
/// Читає з реляційної БД (EF Core + SQLite) дані, які NosqlDemo переносить у MongoDB.
/// Винесено в окремий файл, бо EF Core і MongoDB.Driver мають однойменні методи розширення (ToListAsync).
/// </summary>
internal static class RelationalSnapshot
{
    public static async Task<(List<EmployeeModel> Employees, List<GameProjectModel> Projects)> LoadAsync(string databasePath)
    {
        await using GameStudioContext context = GameStudioContext.CreateSqlite(databasePath);

        List<EmployeeModel> employees = await context.Employees
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync();

        // звичайний (tracking) запит: автозавантаження BugReports → GameProjects утворює цикл, який EF дозволяє лише з відстеженням
        List<GameProjectModel> projects = await context.GameProjects
            .Include(p => p.Bugs).ThenInclude(b => b.Assignee)
            .OrderBy(p => p.Id)
            .ToListAsync();

        return (employees, projects);
    }
}
