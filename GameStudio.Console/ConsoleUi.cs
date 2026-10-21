namespace GameStudio.ConsoleApp;

/// <summary>
/// Допоміжні методи форматованого виводу в консоль (спільні для демонстрацій ЛР1 і ЛР2).
/// </summary>
internal static class ConsoleUi
{
    private const int LineWidth = 100;

    // метод — заголовок розділу
    public static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', LineWidth));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', LineWidth));
    }

    // метод — підзаголовок усередині розділу
    public static void PrintSubsection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {title}");
    }

    // узагальнений (generic) метод виводу будь-якої колекції
    public static void PrintAll<T>(string caption, IEnumerable<T> items)
    {
        Console.WriteLine(caption);
        int index = 1;
        foreach (T item in items)
        {
            Console.WriteLine($"  {index++}. {item}");
        }
    }

    // метод — шапка таблиці статистики
    public static void PrintStatsHeader()
    {
        Console.WriteLine($"  {"Показник",-36} {"Мін",14} {"Макс",14} {"Середнє",14}");
        Console.WriteLine("  " + new string('-', 81));
    }

    // метод — рядок таблиці статистики для грошових (decimal) значень
    public static void PrintStatsRow(string name, decimal min, decimal max, decimal average, string format = "N2")
    {
        Console.WriteLine($"  {name,-36} {min.ToString(format),14} {max.ToString(format),14} {average.ToString(format),14}");
    }

    // метод — рядок таблиці статистики для числових (double) значень
    public static void PrintStatsRow(string name, double min, double max, double average, string format = "N1")
    {
        Console.WriteLine($"  {name,-36} {min.ToString(format),14} {max.ToString(format),14} {average.ToString(format),14}");
    }
}
