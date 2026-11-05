using System.ComponentModel.DataAnnotations;
using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.REST.Mapping;
using GameStudio.REST.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameStudio.REST.Controllers;

/// <summary>Ресурс «художники»: /api/artists (таблиці Employees + Artists, TPT).</summary>
[Route("api/artists")]
public class ArtistsController : ApiControllerBase
{
    private const string ArtistName = "Художника";

    private readonly ICrudServiceAsync<Artist> _artists;

    public ArtistsController(ICrudServiceAsync<Artist> artists)
    {
        _artists = artists;
    }

    /// <summary>Список художників посторінково.</summary>
    /// <param name="page">Номер сторінки (з 1).</param>
    /// <param name="amount">Кількість художників на сторінці (1–100).</param>
    /// <response code="200">Сторінка списку.</response>
    /// <response code="400">Некоректні параметри пагінації.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ArtistModel>>> GetAll(
        [FromQuery, Range(1, int.MaxValue, ErrorMessage = "Номер сторінки має бути не менше 1.")] int page = 1,
        [FromQuery, Range(1, 100, ErrorMessage = "Розмір сторінки має бути від 1 до 100.")] int amount = 10)
    {
        IEnumerable<Artist> artists = await _artists.ReadAllAsync(page, amount);
        return Ok(artists.Select(a => a.ToModel()));
    }

    /// <summary>Один художник за ідентифікатором.</summary>
    /// <response code="200">Художника знайдено.</response>
    /// <response code="404">Художника з таким Id немає.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtistModel>> GetById(Guid id)
    {
        Artist? artist = await FindAsync(_artists, id);
        return artist is null ? NotFoundProblem(ArtistName, id) : Ok(artist.ToModel());
    }

    /// <summary>Приймає на роботу нового художника.</summary>
    /// <response code="201">Художника створено.</response>
    /// <response code="400">Некоректні дані.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArtistModel>> Create(ArtistRequestModel model)
    {
        Artist artist = model.ToDomain();

        await _artists.CreateAsync(artist);
        if (!await _artists.SaveAsync())
        {
            return SaveFailedProblem();
        }

        return CreatedAtAction(nameof(GetById), new { id = artist.Id }, artist.ToModel());
    }

    /// <summary>Оновлює дані художника.</summary>
    /// <response code="200">Дані оновлено.</response>
    /// <response code="400">Некоректні дані.</response>
    /// <response code="404">Художника з таким Id немає.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtistModel>> Update(Guid id, ArtistRequestModel model)
    {
        Artist? artist = await FindAsync(_artists, id);
        if (artist is null)
        {
            return NotFoundProblem(ArtistName, id);
        }

        model.CopyTo(artist);
        await _artists.UpdateAsync(artist);
        return await _artists.SaveAsync() ? Ok(artist.ToModel()) : SaveFailedProblem();
    }

    /// <summary>Звільняє художника (його також вилучено з команд проєктів).</summary>
    /// <response code="204">Художника видалено.</response>
    /// <response code="404">Художника з таким Id немає.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        Artist? artist = await FindAsync(_artists, id);
        if (artist is null)
        {
            return NotFoundProblem(ArtistName, id);
        }

        await _artists.RemoveAsync(artist);
        return await _artists.SaveAsync() ? NoContent() : SaveFailedProblem();
    }
}
