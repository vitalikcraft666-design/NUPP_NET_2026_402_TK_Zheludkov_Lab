using System.Collections.Concurrent;
using GameStudio.Common.Models;

namespace GameStudio.Tests;

/// <summary>
/// Модульні тести для статичних методів CreateNew, які генерують об'єкти з випадковими даними.
/// </summary>
[TestFixture]
public class CreateNewTests
{
    [Test]
    public void Programmer_CreateNew_GeneratesValidData()
    {
        Programmer programmer = Programmer.CreateNew();

        Assert.That(programmer.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(programmer.FullName, Is.Not.Empty);
        Assert.That(programmer.PrimaryLanguage, Is.Not.Empty);
        Assert.That(programmer.BaseSalary, Is.InRange(20_000m, 60_000m));
        Assert.That(programmer.CommitsCount, Is.InRange(0, 5000));
        Assert.That(programmer.HireDate, Is.LessThanOrEqualTo(DateTime.Today));
    }

    [Test]
    public void Artist_CreateNew_GeneratesValidData()
    {
        Artist artist = Artist.CreateNew();

        Assert.That(artist.FullName, Is.Not.Empty);
        Assert.That(artist.MainTool, Is.Not.Empty);
        Assert.That(artist.BaseSalary, Is.InRange(18_000m, 50_000m));
        Assert.That(artist.ArtworksCompleted, Is.InRange(0, 40));
        Assert.That(artist.CalculateMonthlySalary(), Is.GreaterThanOrEqualTo(artist.BaseSalary));
    }

    [Test]
    public void GameProject_CreateNew_GeneratesValidData()
    {
        GameProject project = GameProject.CreateNew();

        Assert.That(project.Title, Is.Not.Empty);
        Assert.That(GameProject.IsEngineSupported(project.Engine), Is.True);
        Assert.That(project.Budget, Is.InRange(50_000m, 2_000_000m));
        Assert.That(project.Spent, Is.InRange(0m, project.Budget * 1.2m));
        Assert.That(project.TeamMemberIds, Has.Count.InRange(2, 15));
    }

    [Test]
    public void BugReport_CreateNew_WithProjectId_LinksBugToProject()
    {
        Guid projectId = Guid.NewGuid();

        BugReport bug = BugReport.CreateNew(projectId);

        Assert.That(bug.ProjectId, Is.EqualTo(projectId));
        Assert.That(bug.Title, Is.Not.Empty);
        Assert.That(bug.CreatedAt, Is.InRange(DateTime.Today.AddDays(-90), DateTime.Today));
        Assert.That(bug.IsFixed && bug.AssigneeId is null, Is.False, "виправлений баг повинен мати виконавця");
    }

    [Test]
    public void CreateNew_CalledInParallel_ProducesUniqueObjects()
    {
        var programmers = new ConcurrentBag<Programmer>();

        Parallel.For(0, 1000, _ => programmers.Add(Programmer.CreateNew()));

        Assert.That(programmers, Has.Count.EqualTo(1000));
        Assert.That(programmers.Select(p => p.Id).Distinct().Count(), Is.EqualTo(1000));
    }

    [Test]
    public void Employee_CreatedCounter_IsThreadSafe()
    {
        int before = Employee.GetCreatedCount();

        Parallel.For(0, 10_000, _ => Artist.CreateNew());

        Assert.That(Employee.GetCreatedCount() - before, Is.EqualTo(10_000));
    }
}
