using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.Infrastructure.Mapping;

/// <summary>
/// Перетворення BugReport ↔ BugReportModel. Доменний баг посилається на проєкт і виконавця через Guid,
/// а в БД це зовнішні ключі GameProjectId і AssigneeId, тому пов'язані записи шукаються за альтернативним ключем.
/// </summary>
public class BugReportMapper : IModelMapper<BugReport, BugReportModel>
{
    private readonly GameStudioContext _context;

    public BugReportMapper(GameStudioContext context)
    {
        _context = context;
    }

    public BugReport ToDomain(BugReportModel model)
    {
        return new BugReport(model.Title, model.Severity, model.GameProject.PublicId)
        {
            Id = model.PublicId,
            CreatedAt = model.CreatedAt,
            AssigneeId = model.Assignee?.PublicId,
            IsFixed = model.IsFixed
        };
    }

    public async Task<BugReportModel> ToModelAsync(BugReport domain)
    {
        var model = new BugReportModel { PublicId = domain.Id };
        await CopyAsync(domain, model);
        return model;
    }

    public Task UpdateModelAsync(BugReportModel model, BugReport domain) => CopyAsync(domain, model);

    private async Task CopyAsync(BugReport domain, BugReportModel model)
    {
        model.Title = domain.Title;
        model.Severity = domain.Severity;
        model.IsFixed = domain.IsFixed;
        model.CreatedAt = domain.CreatedAt;

        if (model.GameProject is null || model.GameProject.PublicId != domain.ProjectId)
        {
            model.GameProject = await _context.GameProjects.FirstOrDefaultAsync(p => p.PublicId == domain.ProjectId)
                                ?? throw new InvalidOperationException($"Проєкт з Id {domain.ProjectId} не знайдено в БД.");
        }

        if (domain.AssigneeId is null)
        {
            model.Assignee = null;
            model.AssigneeId = null;
        }
        else if (model.Assignee?.PublicId != domain.AssigneeId)
        {
            model.Assignee = await _context.Programmers.FirstOrDefaultAsync(p => p.PublicId == domain.AssigneeId);
        }
    }
}
