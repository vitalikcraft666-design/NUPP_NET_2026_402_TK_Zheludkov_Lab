namespace GameStudio.Common.Security;

/// <summary>
/// ЛР5: ролі користувачів ігрової студії (таблиця AspNetRoles). Права зростають від Tester до Admin:
/// <list type="bullet">
/// <item><b>Tester</b> — базова роль кожного зареєстрованого користувача: повідомляє про баги;</item>
/// <item><b>Developer</b> — розробник: бере баги в роботу та позначає виправленими;</item>
/// <item><b>Manager</b> — продюсер: керує проєктами, сторінками в магазині, командами, бачить дані працівників;</item>
/// <item><b>Admin</b> — адміністратор: працівники (прийом / звільнення), видалення проєктів, ролі користувачів.</item>
/// </list>
/// </summary>
public static class StudioRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Developer = "Developer";
    public const string Tester = "Tester";

    // комбінації для атрибутів [Authorize(Roles = ...)]: роль через кому означає «будь-яка з перелічених»
    public const string AdminOrManager = Admin + "," + Manager;
    public const string Staff = Admin + "," + Manager + "," + Developer;

    public static readonly IReadOnlyList<string> All = new[] { Admin, Manager, Developer, Tester };

    // роль, яку отримує кожен новий користувач під час реєстрації
    public const string Default = Tester;
}
