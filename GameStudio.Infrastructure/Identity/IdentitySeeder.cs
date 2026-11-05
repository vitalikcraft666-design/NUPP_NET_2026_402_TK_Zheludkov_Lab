using GameStudio.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GameStudio.Infrastructure.Identity;

/// <summary>
/// ЛР5: початкові дані Identity під час запуску застосунку — усі ролі (RoleManager) і перший адміністратор
/// (UserManager). Пароль адміністратора не зберігається в коді: він береться з конфігурації
/// (змінна середовища Seed__AdminPassword), тому на сервері його задають у налаштуваннях хостингу.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<StudioUser> userManager,
        string? adminEmail, string? adminPassword, ILogger logger)
    {
        foreach (string role in StudioRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Створено роль {Role}", role);
            }
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Адміністратора не створено: не задано Seed:AdminEmail / Seed:AdminPassword.");
            return;
        }

        StudioUser? admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new StudioUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, FullName = "Адміністратор студії" };
            IdentityResult created = await userManager.CreateAsync(admin, adminPassword);
            if (!created.Succeeded)
            {
                logger.LogError("Не вдалося створити адміністратора: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Створено адміністратора {Email}", adminEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, StudioRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, StudioRoles.Admin);
        }
    }
}
