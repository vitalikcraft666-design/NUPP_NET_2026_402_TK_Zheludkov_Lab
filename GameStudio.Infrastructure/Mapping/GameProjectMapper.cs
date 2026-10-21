using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.Infrastructure.Mapping;

/// <summary>
/// Перетворення GameProject ↔ GameProjectModel разом зі зв'язками:
/// StorePage (один-до-одного) і TeamMemberIds ↔ ProjectAssignments (багато-до-багатьох).
/// </summary>
public class GameProjectMapper : IModelMapper<GameProject, GameProjectModel>
{
    private readonly GameStudioContext _context;

    public GameProjectMapper(GameStudioContext context)
    {
        _context = context;
    }

    public GameProject ToDomain(GameProjectModel model)
    {
        var project = new GameProject(model.Title, model.Genre, model.Engine, model.Budget) { Id = model.PublicId };

        if (model.Spent > 0)
        {
            project.Spend(model.Spent);
        }

        if (model.Status != ProjectStatus.Concept)
        {
            project.ChangeStatus(model.Status);
        }

        project.TeamMemberIds.AddRange(model.Assignments.Select(a => a.Employee.PublicId));

        if (model.StorePage is { } page)
        {
            project.StorePage = new StorePage(page.Url, page.Price, page.ReleaseDate) { WishlistCount = page.WishlistCount };
        }

        return project;
    }

    public async Task<GameProjectModel> ToModelAsync(GameProject domain)
    {
        var model = new GameProjectModel { PublicId = domain.Id };
        await CopyAsync(domain, model);
        return model;
    }

    public Task UpdateModelAsync(GameProjectModel model, GameProject domain) => CopyAsync(domain, model);

    private async Task CopyAsync(GameProject domain, GameProjectModel model)
    {
        model.Title = domain.Title;
        model.Genre = domain.Genre;
        model.Engine = domain.Engine;
        model.Budget = domain.Budget;
        model.Spent = domain.Spent;
        model.Status = domain.Status;

        // один-до-одного: сторінка в магазині
        if (domain.StorePage is null)
        {
            model.StorePage = null; // залежний запис буде видалено
        }
        else
        {
            model.StorePage ??= new StorePageModel();
            model.StorePage.Url = domain.StorePage.Url;
            model.StorePage.Price = domain.StorePage.Price;
            model.StorePage.WishlistCount = domain.StorePage.WishlistCount;
            model.StorePage.ReleaseDate = domain.StorePage.ReleaseDate;
        }

        // багато-до-багатьох: синхронізуємо записи ProjectAssignments зі списком TeamMemberIds
        var wanted = domain.TeamMemberIds.ToHashSet();
        model.Assignments.RemoveAll(a => !wanted.Contains(a.Employee.PublicId));

        var present = model.Assignments.Select(a => a.Employee.PublicId).ToHashSet();
        List<Guid> missing = wanted.Where(id => !present.Contains(id)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        // працівники мають уже бути збережені в БД; невідомі ідентифікатори ігноруються
        List<EmployeeModel> employees = await _context.Employees
            .Where(e => missing.Contains(e.PublicId))
            .ToListAsync();

        foreach (EmployeeModel employee in employees)
        {
            model.Assignments.Add(new ProjectAssignmentModel
            {
                Employee = employee,
                GameProject = model,
                Role = employee is ProgrammerModel ? "Програміст" : "Художник",
                AssignedAt = DateTime.Today
            });
        }
    }
}
