using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Common.Security;
using GameStudio.REST.Mapping;
using GameStudio.REST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStudio.REST.Controllers;

/// <summary>Ресурс «програмісти»: /api/programmers (таблиці Employees + Programmers, TPT).</summary>
[Route("api/programmers")]
[Authorize]
public class ProgrammersController : ApiControllerBase
{
    private const string ProgrammerName = "Програміста";

    private readonly ICrudServiceAsync<Programmer> _programmers;

    public ProgrammersController(ICrudServiceAsync<Programmer> programmers)
    {
        _programmers = programmers;
    }

    /// <summary>Список програмістів посторінково.</summary>
    /// <param name="page">Номер сторінки (з 1).</param>
    /// <param name="amount">Кількість програмістів на сторінці (1–100).</param>
    /// <response code="200">Сторінка списку.</response>
    /// <response code="400">Некоректні параметри пагінації.</response>
    [Authorize(Roles = StudioRoles.Staff)]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ProgrammerModel>>> GetAll(
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Номер сторінки має бути не менше 1.")] int page = 1,
        [FromQuery, Range(1, 100, ErrorMessage = "Розмір сторінки має бути від 1 до 100.")] int amount = 10)
    {
        IEnumerable<Programmer> programmers = await _programmers.ReadAllAsync(page, amount);
        return Ok(programmers.Select(p => p.ToModel()));
    }

    /// <summary>Один програміст за ідентифікатором.</summary>
    /// <response code="200">Програміста знайдено.</response>
    /// <response code="404">Програміста з таким Id немає.</response>
    [Authorize(Roles = StudioRoles.Staff)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgrammerModel>> GetById(Guid id)
    {
        Programmer? programmer = await FindAsync(_programmers, id);
        return programmer is null ? NotFoundProblem(ProgrammerName, id) : Ok(programmer.ToModel());
    }

    /// <summary>Приймає на роботу нового програміста.</summary>
    /// <response code="201">Програміста створено.</response>
    /// <response code="400">Некоректні дані.</response>
    [Authorize(Roles = StudioRoles.Admin)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProgrammerModel>> Create(ProgrammerRequestModel model)
    {
        Programmer programmer = model.ToDomain();

        await _programmers.CreateAsync(programmer);
        if (!await _programmers.SaveAsync())
        {
            return SaveFailedProblem();
        }

        return CreatedAtAction(nameof(GetById), new { id = programmer.Id }, programmer.ToModel());
    }

    /// <summary>Оновлює дані програміста.</summary>
    /// <response code="200">Дані оновлено.</response>
    /// <response code="400">Некоректні дані.</response>
    /// <response code="404">Програміста з таким Id немає.</response>
    [Authorize(Roles = StudioRoles.Admin)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgrammerModel>> Update(Guid id, ProgrammerRequestModel model)
    {
        Programmer? programmer = await FindAsync(_programmers, id);
        if (programmer is null)
        {
            return NotFoundProblem(ProgrammerName, id);
        }

        model.CopyTo(programmer);
        await _programmers.UpdateAsync(programmer);
        return await _programmers.SaveAsync() ? Ok(programmer.ToModel()) : SaveFailedProblem();
    }

    /// <summary>Звільняє програміста: його баги залишаються без виконавця, а з команд проєктів його вилучено.</summary>
    /// <response code="204">Програміста видалено.</response>
    /// <response code="404">Програміста з таким Id немає.</response>
    [Authorize(Roles = StudioRoles.Admin)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        Programmer? programmer = await FindAsync(_programmers, id);
        if (programmer is null)
        {
            return NotFoundProblem(ProgrammerName, id);
        }

        await _programmers.RemoveAsync(programmer);
        return await _programmers.SaveAsync() ? NoContent() : SaveFailedProblem();
    }
}
