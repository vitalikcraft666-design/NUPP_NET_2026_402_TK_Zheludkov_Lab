using GameStudio.Common.Models;
using GameStudio.REST.Models;

namespace GameStudio.REST.Mapping;

/// <summary>
/// Перетворення доменних моделей (рівень бізнес-логіки, GameStudio.Common) у моделі API (рівень представлення)
/// і навпаки. Контролери працюють лише з моделями з папки Models, а моделі БД (GameStudio.Infrastructure)
/// їм узагалі не видно — так дотримується трирівнева архітектура.
/// </summary>
public static class ApiModelMapper
{
    // ---------- проєкти ----------

    public static ProjectSummaryModel ToSummaryModel(this GameProject project) => new()
    {
        Id = project.Id,
        Title = project.Title,
        Genre = project.Genre,
        Engine = project.Engine,
        Status = project.Status,
        Budget = project.Budget,
        Spent = project.Spent
    };

    public static ProjectModel ToModel(this GameProject project) => new()
    {
        Id = project.Id,
        Title = project.Title,
        Genre = project.Genre,
        Engine = project.Engine,
        Status = project.Status,
        Budget = project.Budget,
        Spent = project.Spent,
        Remaining = project.Budget - project.Spent,
        TeamSize = project.TeamMemberIds.Count,
        StorePage = project.StorePage?.ToModel()
    };

    public static GameProject ToDomain(this ProjectCreateModel model) =>
        new(model.Title.Trim(), model.Genre.Trim(), model.Engine, model.Budget);

    // ---------- сторінка в магазині ----------

    public static StorePageModel ToModel(this StorePage page) => new()
    {
        Url = page.Url,
        Price = page.Price,
        WishlistCount = page.WishlistCount,
        ReleaseDate = page.ReleaseDate
    };

    public static StorePage ToDomain(this StorePageModel model) =>
        new(model.Url, model.Price, model.ReleaseDate) { WishlistCount = model.WishlistCount };

    // ---------- команда ----------

    public static TeamMemberModel ToTeamMemberModel(this Employee employee) => new()
    {
        Id = employee.Id,
        FullName = employee.FullName,
        Type = employee is Programmer ? "Programmer" : "Artist",
        Position = employee.Position
    };

    // ---------- баги ----------

    public static BugModel ToModel(this BugReport bug) => new()
    {
        Id = bug.Id,
        Title = bug.Title,
        Severity = bug.Severity,
        IsFixed = bug.IsFixed,
        // SQLite не зберігає часовий пояс, тому дата завжди віддається без зміщення — однаково для нових і прочитаних багів
        CreatedAt = DateTime.SpecifyKind(bug.CreatedAt, DateTimeKind.Unspecified),
        ProjectId = bug.ProjectId,
        AssigneeId = bug.AssigneeId
    };

    // ---------- програмісти ----------

    public static ProgrammerModel ToModel(this Programmer programmer) => new()
    {
        Id = programmer.Id,
        FullName = programmer.FullName,
        HireDate = programmer.HireDate,
        BaseSalary = programmer.BaseSalary,
        PrimaryLanguage = programmer.PrimaryLanguage,
        Grade = programmer.Grade,
        CommitsCount = programmer.CommitsCount,
        Position = programmer.Position,
        YearsOfExperience = programmer.GetYearsOfExperience(),
        MonthlySalary = programmer.CalculateMonthlySalary()
    };

    public static Programmer ToDomain(this ProgrammerRequestModel model)
    {
        var programmer = new Programmer();
        model.CopyTo(programmer);
        return programmer;
    }

    public static void CopyTo(this ProgrammerRequestModel model, Programmer programmer)
    {
        programmer.FullName = model.FullName.Trim();
        programmer.HireDate = (model.HireDate ?? DateTime.Today).Date;
        programmer.BaseSalary = model.BaseSalary;
        programmer.PrimaryLanguage = model.PrimaryLanguage.Trim();
        programmer.Grade = model.Grade;
        programmer.CommitsCount = model.CommitsCount;
    }

    // ---------- художники ----------

    public static ArtistModel ToModel(this Artist artist) => new()
    {
        Id = artist.Id,
        FullName = artist.FullName,
        HireDate = artist.HireDate,
        BaseSalary = artist.BaseSalary,
        Specialization = artist.Specialization,
        MainTool = artist.MainTool,
        ArtworksCompleted = artist.ArtworksCompleted,
        Position = artist.Position,
        YearsOfExperience = artist.GetYearsOfExperience(),
        MonthlySalary = artist.CalculateMonthlySalary()
    };

    public static Artist ToDomain(this ArtistRequestModel model)
    {
        var artist = new Artist();
        model.CopyTo(artist);
        return artist;
    }

    public static void CopyTo(this ArtistRequestModel model, Artist artist)
    {
        artist.FullName = model.FullName.Trim();
        artist.HireDate = (model.HireDate ?? DateTime.Today).Date;
        artist.BaseSalary = model.BaseSalary;
        artist.Specialization = model.Specialization;
        artist.MainTool = model.MainTool.Trim();
        artist.ArtworksCompleted = model.ArtworksCompleted;
    }
}
