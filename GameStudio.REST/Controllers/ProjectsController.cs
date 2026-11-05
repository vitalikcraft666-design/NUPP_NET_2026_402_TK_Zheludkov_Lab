using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.REST.Mapping;
using GameStudio.REST.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStudio.REST.Controllers;

/// <summary>
/// Ресурс «ігрові проєкти»: /api/projects.
/// Вкладені ресурси: сторінка в магазині (1:1), баги проєкту (1:N) і команда (N:M).
/// </summary>
[Route("api/projects")]
public class ProjectsController : ApiControllerBase
{
    private const string ProjectName = "Проєкт";

    private readonly ICrudServiceAsync<GameProject> _projects;
    private readonly ICrudServiceAsync<BugReport> _bugs;
    private readonly ICrudServiceAsync<Employee> _employees;

    // залежності (CRUD-сервіси) передає контейнер впровадження залежностей ASP.NET Core
    public ProjectsController(ICrudServiceAsync<GameProject> projects, ICrudServiceAsync<BugReport> bugs,
        ICrudServiceAsync<Employee> employees)
    {
        _projects = projects;
        _bugs = bugs;
        _employees = employees;
    }

    /// <summary>Список проєктів посторінково.</summary>
    /// <param name="page">Номер сторінки (з 1).</param>
    /// <param name="amount">Кількість проєктів на сторінці (1–100).</param>
    /// <response code="200">Сторінка списку (порожній масив, якщо сторінка за межами списку).</response>
    /// <response code="400">Некоректні параметри пагінації.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ProjectSummaryModel>>> GetAll(
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Номер сторінки має бути не менше 1.")] int page = 1,
        [FromQuery, Range(1, 100, ErrorMessage = "Розмір сторінки має бути від 1 до 100.")] int amount = 10)
    {
        IEnumerable<GameProject> projects = await _projects.ReadAllAsync(page, amount);
        return Ok(projects.Select(p => p.ToSummaryModel()));
    }

    /// <summary>Один проєкт за ідентифікатором.</summary>
    /// <response code="200">Проєкт знайдено.</response>
    /// <response code="404">Проєкту з таким Id немає.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectModel>> GetById(Guid id)
    {
        GameProject? project = await FindAsync(_projects, id);
        return project is null ? NotFoundProblem(ProjectName, id) : Ok(project.ToModel());
    }

    /// <summary>Створює новий проєкт (статус Concept).</summary>
    /// <response code="201">Проєкт створено; заголовок Location містить його адресу.</response>
    /// <response code="400">Некоректні дані (наприклад, непідтримуваний рушій).</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectModel>> Create(ProjectCreateModel model)
    {
        GameProject project = model.ToDomain();

        await _projects.CreateAsync(project);
        if (!await _projects.SaveAsync())
        {
            return SaveFailedProblem();
        }

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project.ToModel());
    }

    /// <summary>Оновлює назву, жанр, рушій, бюджет і статус проєкту.</summary>
    /// <response code="200">Проєкт оновлено.</response>
    /// <response code="400">Некоректні дані.</response>
    /// <response code="404">Проєкту з таким Id немає.</response>
    /// <response code="409">Статус завершеного (Released / Cancelled) проєкту змінити не можна.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectModel>> Update(Guid id, ProjectUpdateModel model)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        if (model.Status != project.Status)
        {
            try
            {
                project.ChangeStatus(model.Status);
            }
            catch (InvalidOperationException ex)
            {
                return ConflictProblem(ex.Message);
            }
        }

        project.Title = model.Title.Trim();
        project.Genre = model.Genre.Trim();
        project.Engine = model.Engine;
        project.Budget = model.Budget;

        await _projects.UpdateAsync(project);
        return await _projects.SaveAsync() ? Ok(project.ToModel()) : SaveFailedProblem();
    }

    /// <summary>Видаляє проєкт разом зі сторінкою в магазині, багами та записами про команду (каскадно).</summary>
    /// <response code="204">Проєкт видалено.</response>
    /// <response code="404">Проєкту з таким Id немає.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        await _projects.RemoveAsync(project);
        return await _projects.SaveAsync() ? NoContent() : SaveFailedProblem();
    }

    // ===================== сторінка в магазині (один-до-одного) =====================

    /// <summary>Сторінка проєкту в магазині.</summary>
    /// <response code="200">Сторінка є.</response>
    /// <response code="404">Немає проєкту або в нього ще немає сторінки.</response>
    [HttpGet("{id:guid}/store-page")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorePageModel>> GetStorePage(Guid id)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        return project.StorePage is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Не знайдено",
                detail: $"У проєкту \"{project.Title}\" ще немає сторінки в магазині.")
            : Ok(project.StorePage.ToModel());
    }

    /// <summary>Створює або повністю замінює сторінку проєкту в магазині.</summary>
    /// <response code="201">Сторінку створено.</response>
    /// <response code="200">Наявну сторінку замінено.</response>
    /// <response code="400">Некоректні дані.</response>
    /// <response code="404">Проєкту з таким Id немає.</response>
    [HttpPut("{id:guid}/store-page")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorePageModel>> PutStorePage(Guid id, StorePageModel model)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        bool created = project.StorePage is null;
        project.StorePage = model.ToDomain();

        await _projects.UpdateAsync(project);
        if (!await _projects.SaveAsync())
        {
            return SaveFailedProblem();
        }

        StorePageModel result = project.StorePage.ToModel();
        return created ? CreatedAtAction(nameof(GetStorePage), new { id }, result) : Ok(result);
    }

    /// <summary>Видаляє сторінку проєкту з магазину.</summary>
    /// <response code="204">Сторінку видалено.</response>
    /// <response code="404">Немає проєкту або сторінки.</response>
    [HttpDelete("{id:guid}/store-page")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStorePage(Guid id)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        if (project.StorePage is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Не знайдено",
                detail: $"У проєкту \"{project.Title}\" немає сторінки в магазині.");
        }

        project.StorePage = null;
        await _projects.UpdateAsync(project);
        return await _projects.SaveAsync() ? NoContent() : SaveFailedProblem();
    }

    // ===================== баги проєкту (один-до-багатьох) =====================

    /// <summary>Усі баги проєкту.</summary>
    /// <response code="200">Список багів (може бути порожнім).</response>
    /// <response code="404">Проєкту з таким Id немає.</response>
    [HttpGet("{id:guid}/bugs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<BugModel>>> GetBugs(Guid id)
    {
        if (await FindAsync(_projects, id) is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        IEnumerable<BugReport> bugs = await _bugs.ReadAllAsync();
        return Ok(bugs.Where(b => b.ProjectId == id)
            .OrderByDescending(b => b.Severity)
            .ThenBy(b => b.CreatedAt)
            .Select(b => b.ToModel()));
    }

    // ===================== команда проєкту (багато-до-багатьох) =====================

    /// <summary>Склад команди проєкту.</summary>
    /// <response code="200">Учасники команди.</response>
    /// <response code="404">Проєкту з таким Id немає.</response>
    [HttpGet("{id:guid}/team")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TeamMemberModel>>> GetTeam(Guid id)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        var team = new List<TeamMemberModel>();
        foreach (Guid employeeId in project.TeamMemberIds)
        {
            if (await FindAsync(_employees, employeeId) is { } employee)
            {
                team.Add(employee.ToTeamMemberModel());
            }
        }

        return Ok(team.OrderBy(m => m.Type).ThenBy(m => m.FullName));
    }

    /// <summary>Додає працівника (програміста або художника) до команди проєкту. Повторний виклик нічого не змінює.</summary>
    /// <response code="204">Працівник у команді.</response>
    /// <response code="404">Немає проєкту або працівника.</response>
    [HttpPut("{id:guid}/team/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTeamMember(Guid id, Guid employeeId)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        Employee? employee = await FindAsync(_employees, employeeId);
        if (employee is null)
        {
            return NotFoundProblem("Працівника", employeeId);
        }

        if (project.TeamMemberIds.Contains(employeeId))
        {
            return NoContent();
        }

        project.AddTeamMember(employee);
        await _projects.UpdateAsync(project);
        return await _projects.SaveAsync() ? NoContent() : SaveFailedProblem();
    }

    /// <summary>Вилучає працівника з команди проєкту.</summary>
    /// <response code="204">Працівника вилучено.</response>
    /// <response code="404">Немає проєкту або працівник не входить до команди.</response>
    [HttpDelete("{id:guid}/team/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTeamMember(Guid id, Guid employeeId)
    {
        GameProject? project = await FindAsync(_projects, id);
        if (project is null)
        {
            return NotFoundProblem(ProjectName, id);
        }

        if (!project.TeamMemberIds.Remove(employeeId))
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Не знайдено",
                detail: $"Працівник з Id {employeeId} не входить до команди проєкту \"{project.Title}\".");
        }

        await _projects.UpdateAsync(project);
        return await _projects.SaveAsync() ? NoContent() : SaveFailedProblem();
    }
}
