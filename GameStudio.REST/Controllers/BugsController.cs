using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.REST.Mapping;
using GameStudio.REST.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStudio.REST.Controllers;

/// <summary>Ресурс «баги»: /api/bugs. Кожен баг належить проєкту і може бути призначений програмісту.</summary>
[Route("api/bugs")]
public class BugsController : ApiControllerBase
{
    private const string BugName = "Баг";

    private readonly ICrudServiceAsync<BugReport> _bugs;
    private readonly ICrudServiceAsync<GameProject> _projects;
    private readonly ICrudServiceAsync<Programmer> _programmers;

    public BugsController(ICrudServiceAsync<BugReport> bugs, ICrudServiceAsync<GameProject> projects,
        ICrudServiceAsync<Programmer> programmers)
    {
        _bugs = bugs;
        _projects = projects;
        _programmers = programmers;
    }

    /// <summary>Список багів посторінково.</summary>
    /// <param name="page">Номер сторінки (з 1).</param>
    /// <param name="amount">Кількість багів на сторінці (1–100).</param>
    /// <response code="200">Сторінка списку.</response>
    /// <response code="400">Некоректні параметри пагінації.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<BugModel>>> GetAll(
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Номер сторінки має бути не менше 1.")] int page = 1,
        [FromQuery, Range(1, 100, ErrorMessage = "Розмір сторінки має бути від 1 до 100.")] int amount = 10)
    {
        IEnumerable<BugReport> bugs = await _bugs.ReadAllAsync(page, amount);
        return Ok(bugs.Select(b => b.ToModel()));
    }

    /// <summary>Один баг за ідентифікатором.</summary>
    /// <response code="200">Баг знайдено.</response>
    /// <response code="404">Бага з таким Id немає.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BugModel>> GetById(Guid id)
    {
        BugReport? bug = await FindAsync(_bugs, id);
        return bug is null ? NotFoundProblem(BugName, id) : Ok(bug.ToModel());
    }

    /// <summary>Реєструє новий баг у проєкті.</summary>
    /// <response code="201">Баг створено.</response>
    /// <response code="400">Некоректні дані або посилання на неіснуючий проєкт / програміста.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BugModel>> Create(BugCreateModel model)
    {
        Guid projectId = model.ProjectId!.Value;
        if (await FindAsync(_projects, projectId) is null)
        {
            return FieldValidationProblem(nameof(model.ProjectId), $"Проєкт з Id {projectId} не існує.");
        }

        if (model.AssigneeId is { } assigneeId && await FindAsync(_programmers, assigneeId) is null)
        {
            return FieldValidationProblem(nameof(model.AssigneeId), $"Програміста з Id {assigneeId} не існує.");
        }

        var bug = new BugReport(model.Title.Trim(), model.Severity, projectId) { AssigneeId = model.AssigneeId };

        await _bugs.CreateAsync(bug);
        if (!await _bugs.SaveAsync())
        {
            return SaveFailedProblem();
        }

        return CreatedAtAction(nameof(GetById), new { id = bug.Id }, bug.ToModel());
    }

    /// <summary>Оновлює опис, критичність, виконавця та стан бага.</summary>
    /// <response code="200">Баг оновлено.</response>
    /// <response code="400">Некоректні дані (наприклад, виправлений баг без виконавця).</response>
    /// <response code="404">Бага з таким Id немає.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BugModel>> Update(Guid id, BugUpdateModel model)
    {
        BugReport? bug = await FindAsync(_bugs, id);
        if (bug is null)
        {
            return NotFoundProblem(BugName, id);
        }

        if (model.AssigneeId is { } assigneeId && await FindAsync(_programmers, assigneeId) is null)
        {
            return FieldValidationProblem(nameof(model.AssigneeId), $"Програміста з Id {assigneeId} не існує.");
        }

        if (model.IsFixed && model.AssigneeId is null)
        {
            return FieldValidationProblem(nameof(model.IsFixed), "Баг не можна позначити виправленим, доки його нікому не призначено.");
        }

        bug.Title = model.Title.Trim();
        bug.Severity = model.Severity;
        bug.AssigneeId = model.AssigneeId;
        if (model.IsFixed)
        {
            bug.MarkAsFixed();
        }
        else
        {
            bug.IsFixed = false;
        }

        await _bugs.UpdateAsync(bug);
        return await _bugs.SaveAsync() ? Ok(bug.ToModel()) : SaveFailedProblem();
    }

    /// <summary>Видаляє баг.</summary>
    /// <response code="204">Баг видалено.</response>
    /// <response code="404">Бага з таким Id немає.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        BugReport? bug = await FindAsync(_bugs, id);
        if (bug is null)
        {
            return NotFoundProblem(BugName, id);
        }

        await _bugs.RemoveAsync(bug);
        return await _bugs.SaveAsync() ? NoContent() : SaveFailedProblem();
    }
}
