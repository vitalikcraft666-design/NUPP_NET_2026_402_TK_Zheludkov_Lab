using GameStudio.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameStudio.Infrastructure.Identity;

/// <summary>
/// ЛР5: менеджер користувачів на основі UserManager&lt;StudioUser&gt; з ASP.NET Core Identity.
/// Вбудована кінцева точка /register викликає UserManager.CreateAsync, тому тут новий користувач
/// отримує дату реєстрації та роль за замовчуванням (Tester) — без зміни вбудованих кінцевих точок.
/// </summary>
public class StudioUserManager : UserManager<StudioUser>
{
    public StudioUserManager(
        IUserStore<StudioUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<StudioUser> passwordHasher,
        IEnumerable<IUserValidator<StudioUser>> userValidators,
        IEnumerable<IPasswordValidator<StudioUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<StudioUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    // викликається і з CreateAsync(user, password) — після обчислення хешу пароля
    public override async Task<IdentityResult> CreateAsync(StudioUser user)
    {
        if (user.RegisteredAt == default)
        {
            user.RegisteredAt = DateTime.UtcNow;
        }

        IdentityResult result = await base.CreateAsync(user);
        if (!result.Succeeded)
        {
            return result;
        }

        return await AddToRoleAsync(user, StudioRoles.Default);
    }
}
