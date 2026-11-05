using GameStudio.Infrastructure.Identity;
using GameStudio.REST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GameStudio.REST.Controllers;

/// <summary>
/// ЛР5: профіль поточного користувача: /api/account/me. Доступний будь-якому автентифікованому користувачу —
/// хто саме робить запит, визначається за токеном у заголовку Authorization.
/// </summary>
[Route("api/account")]
[Authorize]
public class AccountController : ApiControllerBase
{
    private readonly UserManager<StudioUser> _userManager;

    public AccountController(UserManager<StudioUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Дані поточного користувача та його ролі.</summary>
    /// <response code="200">Профіль користувача.</response>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountModel>> GetMe()
    {
        StudioUser? user = await _userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToModelAsync(_userManager, user));
    }

    /// <summary>Змінює ПІБ поточного користувача.</summary>
    /// <response code="200">Профіль оновлено.</response>
    /// <response code="400">Некоректні дані.</response>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountModel>> UpdateMe(AccountUpdateModel model)
    {
        StudioUser? user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        user.FullName = model.FullName.Trim();
        IdentityResult result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Ok(await ToModelAsync(_userManager, user)) : SaveFailedProblem();
    }

    internal static async Task<AccountModel> ToModelAsync(UserManager<StudioUser> userManager, StudioUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        RegisteredAt = user.RegisteredAt,
        Roles = (await userManager.GetRolesAsync(user)).OrderBy(r => r).ToList()
    };
}
