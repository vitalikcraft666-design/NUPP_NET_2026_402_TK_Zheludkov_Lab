using GameStudio.Common.Generation;

namespace GameStudio.Common.Models;

/// <summary>
/// ЛР3: сторінка гри в цифровому магазині. Кожен проєкт має не більше однієї сторінки,
/// а кожна сторінка належить рівно одному проєкту — зв'язок «один-до-одного».
/// </summary>
public class StorePage
{
    // конструктор без параметрів (потрібен для десеріалізації з JSON)
    public StorePage()
    {
        Url = string.Empty;
    }

    // конструктор з параметрами
    public StorePage(string url, decimal price, DateTime? releaseDate)
    {
        Url = url;
        Price = price;
        ReleaseDate = releaseDate;
    }

    public string Url { get; set; }

    public decimal Price { get; set; }

    public int WishlistCount { get; set; }

    public DateTime? ReleaseDate { get; set; }

    // статичний фабричний метод — сторінка з випадково згенерованими даними для гри з назвою gameTitle
    public static StorePage CreateNew(string gameTitle, bool released)
    {
        string slug = string.Concat(gameTitle.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        DateTime releaseDate = released
            ? RandomData.Date(DateTime.Today.AddYears(-3), DateTime.Today)
            : RandomData.Date(DateTime.Today.AddMonths(1), DateTime.Today.AddYears(2));

        return new StorePage($"https://store.gamestudio.ua/games/{slug}-{Random.Shared.Next(100, 1000)}",
            RandomData.Money(0m, 1499m, 1m), releaseDate)
        {
            WishlistCount = Random.Shared.Next(0, 250_001)
        };
    }

    public override string ToString()
    {
        string release = ReleaseDate is null ? "дата виходу невідома" : $"вихід {ReleaseDate:dd.MM.yyyy}";
        return $"{Url} — {Price:N0} грн, у бажаному: {WishlistCount:N0}, {release}";
    }
}
