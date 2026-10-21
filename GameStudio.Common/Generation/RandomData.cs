namespace GameStudio.Common.Generation;

/// <summary>
/// Допоміжний клас для генерації випадкових тестових даних (ЛР2, методи CreateNew).
/// Використовує Random.Shared — потокобезпечний екземпляр Random (.NET 6+),
/// тому методи можна викликати одночасно з багатьох потоків (наприклад, з Parallel.For).
/// </summary>
internal static class RandomData
{
    private static readonly string[] FirstNames =
    {
        "Андрій", "Олена", "Максим", "Ірина", "Дмитро", "Софія", "Богдан", "Анна", "Тарас", "Марія",
        "Олександр", "Юлія", "Назар", "Катерина", "Владислав", "Дарина", "Роман", "Вікторія", "Артем", "Наталія"
    };

    private static readonly string[] LastNames =
    {
        "Коваленко", "Шевчук", "Бондар", "Мельник", "Ткаченко", "Кравченко", "Олійник", "Бойко", "Лисенко", "Гнатюк",
        "Павленко", "Савчук", "Руденко", "Мороз", "Поліщук", "Зінченко", "Литвин", "Марченко", "Кузьменко", "Романюк"
    };

    private static readonly string[] TitleFirstWords =
    {
        "Sky", "Neon", "Pixel", "Shadow", "Iron", "Crystal", "Cyber", "Lost", "Tiny", "Star", "Wild", "Frozen"
    };

    private static readonly string[] TitleSecondWords =
    {
        "Pirates", "Farm", "Racer", "Knights", "Legends", "Quest", "Defense", "Tactics", "Islands", "Odyssey", "Arena", "Dungeon"
    };

    private static readonly string[] BugObjects =
    {
        "Гравець", "Камера", "Інвентар", "Мінікарта", "Головне меню", "Збереження", "Діалог з NPC", "Звук пострілу", "Анімація стрибка", "Таблиця рекордів"
    };

    private static readonly string[] BugProblems =
    {
        "зависає після паузи", "не відображається на 4K", "провалюється крізь текстури", "дублює предмети",
        "скидається після виходу", "мерехтить у темних локаціях", "не реагує на геймпад", "показує неправильний текст"
    };

    public static readonly string[] ProgrammingLanguages = { "C#", "C++", "Luau", "Python", "JavaScript", "Rust", "Go", "GDScript" };

    public static readonly string[] ArtTools = { "Blender", "Maya", "Photoshop", "Figma", "Spine", "Substance Painter", "ZBrush", "Krita" };

    public static readonly string[] Genres = { "Action RPG", "Simulator", "Racing", "Platformer", "Tower Defense", "Strategy", "Horror", "Puzzle" };

    // метод — випадковий елемент масиву
    public static T Pick<T>(T[] items) => items[Random.Shared.Next(items.Length)];

    // метод — випадкове значення перерахування (enum)
    public static TEnum PickEnum<TEnum>()
        where TEnum : struct, Enum
    {
        TEnum[] values = Enum.GetValues<TEnum>();
        return values[Random.Shared.Next(values.Length)];
    }

    public static string FullName() => $"{Pick(FirstNames)} {Pick(LastNames)}";

    public static string GameTitle() => $"{Pick(TitleFirstWords)} {Pick(TitleSecondWords)}";

    public static string BugTitle() => $"{Pick(BugObjects)} {Pick(BugProblems)}";

    // метод — випадкова сума в діапазоні [min, max], округлена до кроку step
    public static decimal Money(decimal min, decimal max, decimal step)
    {
        decimal value = min + (decimal)Random.Shared.NextDouble() * (max - min);
        return Math.Round(value / step) * step;
    }

    // метод — випадкова дата в діапазоні [from, to]
    public static DateTime Date(DateTime from, DateTime to)
    {
        int days = (to - from).Days;
        return from.AddDays(Random.Shared.Next(days + 1)).Date;
    }
}
