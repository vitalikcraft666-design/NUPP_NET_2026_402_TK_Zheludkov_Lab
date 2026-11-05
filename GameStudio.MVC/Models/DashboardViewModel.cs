namespace GameStudio.MVC.Models;

/// <summary>Модель представлення головної сторінки: кількість записів у таблицях БД.</summary>
public class DashboardViewModel
{
    public int Projects { get; set; }

    public int ReleasedProjects { get; set; }

    public int Programmers { get; set; }

    public int Artists { get; set; }

    public int Bugs { get; set; }

    public int OpenBugs { get; set; }

    public int StorePages { get; set; }
}
