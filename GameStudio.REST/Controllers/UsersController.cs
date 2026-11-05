using GameStudio.Common.Security;
using GameStudio.Infrastructure.Identity;
using GameStudio.REST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStudio.REST.Controllers;

/// <summary>
/// ЛР5: керування користувачами та їхніми ролями: /api/users. Лише для адміністратора.
/// Використовує UserManager (користувачі) і RoleManager (ролі) з ASP.NET Core Identity.
/// </summary>
[Route("api/users")]
[Authorize(Roles = StudioRoles.Admin)]
public class UsersController : ApiControllerBase
{
    private readonly UserManager<StudioUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(UserManager<StudioUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>Усі користувачі з їхніми ролями.</summary>
    /// <response code="200">Список користувачів.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AccountModel>>> GetAll()
    {
        List<StudioUser> users = await _userManager.Users.OrderBy(u => u.RegisteredAt).ToListAsync();
        var result = new List<AccountModel>();
        foreach (StudioUser user in users)
        {
            result.Add(await AccountController.ToModelAsync(_userManager, user));
        }

        return Ok(result);
    }

    /// <summary>Наявні ролі.</summary>
    /// <response code="200">Назви ролей.</response>
    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetRoles()
    {
        return Ok(await _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToListAsync());
    }

    /// <summary>Надає користувачу роль (Admin, Manager, Developer або Tester). Повторний виклик нічого не змінює.</summary>
    /// <response code="200">Користувач з оновленим списком ролей.</response>
    /// <response code="404">Немає користувача або ролі.</response>
    [HttpPut("{id}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountModel>> AddRole(string id, string role)
    {
        StudioUser? user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return UserNotFound(id);
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return RoleNotFound(role);
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            IdentityResult result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                return SaveFailedProblem();
            }
        }

        return Ok(await AccountController.ToModelAsync(_userManager, user));
    }

    /// <summary>Забирає в користувача роль.</summary>
    /// <response code="200">Користувач з оновленим списком ролей.</response>
    /// <response code="404">Немає користувача або ролі.</response>
    /// <response code="409">Адміністратор не може забрати роль Admin сам у себе.</response>
    [HttpDelete("{id}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountModel>> RemoveRole(string id, string role)
    {
        StudioUser? user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return UserNotFound(id);
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return RoleNotFound(role);
        }

        if (role == StudioRoles.Admin && user.Id == _userManager.GetUserId(User))
        {
            return ConflictProblem("Адміністратор не може забрати роль Admin сам у себе.");
        }

        if (await _userManager.IsInRoleAsync(user, role))
        {
            IdentityResult result = await _userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded)
            {
                return SaveFailedProblem();
            }
        }

        return Ok(await AccountController.ToModelAsync(_userManager, user));
    }

    private ObjectResult UserNotFound(string id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Не знайдено", detail: $"Користувача з Id {id} не знайдено.");

    private ObjectResult RoleNotFound(string role) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Не знайдено",
            detail: $"Ролі \"{role}\" не існує. Доступні ролі: {string.Join(", ", StudioRoles.All)}.");
}
