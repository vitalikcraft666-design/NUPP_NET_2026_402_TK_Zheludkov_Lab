using System.Collections.Concurrent;
using GameStudio.Common.Models;
using GameStudio.Common.Services;

namespace GameStudio.Tests;

/// <summary>
/// Модульні тести для CrudServiceAsync&lt;T&gt; (реалізація ICrudServiceAsync&lt;T&gt;).
/// </summary>
[TestFixture]
public class CrudServiceAsyncTests
{
    private string _filePath = null!;
    private CrudServiceAsync<GameProject> _service = null!;

    [SetUp]
    public void SetUp()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"gamestudio-tests-{Guid.NewGuid():N}.json");
        _service = new CrudServiceAsync<GameProject>(_filePath);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    // ---------- CreateAsync ----------

    [Test]
    public async Task CreateAsync_NewElement_ReturnsTrueAndStoresElement()
    {
        GameProject project = NewProject("Sky Pirates");

        bool created = await _service.CreateAsync(project);

        Assert.That(created, Is.True);
        Assert.That(_service.Count, Is.EqualTo(1));
        Assert.That(await _service.ReadAsync(project.Id), Is.SameAs(project));
    }

    [Test]
    public async Task CreateAsync_DuplicateId_ReturnsFalseAndKeepsOriginal()
    {
        GameProject original = NewProject("Original");
        GameProject duplicate = NewProject("Duplicate");
        duplicate.Id = original.Id;
        await _service.CreateAsync(original);

        bool created = await _service.CreateAsync(duplicate);

        Assert.That(created, Is.False);
        Assert.That(_service.Count, Is.EqualTo(1));
        Assert.That((await _service.ReadAsync(original.Id)).Title, Is.EqualTo("Original"));
    }

    [Test]
    public async Task CreateAsync_EmptyId_AssignsNewId()
    {
        GameProject project = NewProject();
        project.Id = Guid.Empty;

        await _service.CreateAsync(project);

        Assert.That(project.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(await _service.ReadAsync(project.Id), Is.SameAs(project));
    }

    [Test]
    public void CreateAsync_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _service.CreateAsync(null!));
    }

    // ---------- ReadAsync / ReadAllAsync ----------

    [Test]
    public void ReadAsync_MissingId_ThrowsKeyNotFoundException()
    {
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.ReadAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task ReadAllAsync_ReturnsAllElementsInInsertionOrder()
    {
        await AddProjectsAsync(5);

        IEnumerable<GameProject> all = await _service.ReadAllAsync();

        Assert.That(all.Select(p => p.Title), Is.EqualTo(new[] { "P0", "P1", "P2", "P3", "P4" }));
    }

    [Test]
    public async Task ReadAllAsync_EmptyService_ReturnsEmptyCollection()
    {
        Assert.That(await _service.ReadAllAsync(), Is.Empty);
    }

    // ---------- пагінація ----------

    [TestCase(1, 3, new[] { 0, 1, 2 })]
    [TestCase(2, 3, new[] { 3, 4, 5 })]
    [TestCase(4, 3, new[] { 9 })]
    [TestCase(5, 3, new int[0])]
    [TestCase(1, 20, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })]
    public async Task ReadAllAsync_Paging_ReturnsExpectedPage(int page, int amount, int[] expectedIndexes)
    {
        await AddProjectsAsync(10);

        IEnumerable<GameProject> pageItems = await _service.ReadAllAsync(page, amount);

        Assert.That(pageItems.Select(p => p.Title), Is.EqualTo(expectedIndexes.Select(i => $"P{i}")));
    }

    [TestCase(0, 5)]
    [TestCase(-1, 5)]
    [TestCase(1, 0)]
    [TestCase(1, -3)]
    public void ReadAllAsync_InvalidPaging_ThrowsArgumentOutOfRangeException(int page, int amount)
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _service.ReadAllAsync(page, amount));
    }

    [TestCase(10, 3, 4)]
    [TestCase(10, 5, 2)]
    [TestCase(0, 5, 0)]
    public async Task GetTotalPages_ReturnsRoundedUpPageCount(int elements, int amount, int expectedPages)
    {
        await AddProjectsAsync(elements);

        Assert.That(_service.GetTotalPages(amount), Is.EqualTo(expectedPages));
    }

    // ---------- UpdateAsync ----------

    [Test]
    public async Task UpdateAsync_ExistingElement_ReplacesItAndKeepsPosition()
    {
        List<GameProject> projects = await AddProjectsAsync(3);
        var replacement = new GameProject("Updated", "Racing", "Unity", 5000m) { Id = projects[1].Id };

        bool updated = await _service.UpdateAsync(replacement);

        Assert.That(updated, Is.True);
        Assert.That((await _service.ReadAllAsync()).Select(p => p.Title), Is.EqualTo(new[] { "P0", "Updated", "P2" }));
    }

    [Test]
    public async Task UpdateAsync_MissingElement_ReturnsFalse()
    {
        await AddProjectsAsync(2);

        bool updated = await _service.UpdateAsync(NewProject("Not stored"));

        Assert.That(updated, Is.False);
        Assert.That(_service.Count, Is.EqualTo(2));
    }

    // ---------- RemoveAsync ----------

    [Test]
    public async Task RemoveAsync_ExistingElement_ReturnsTrueAndDeletesIt()
    {
        List<GameProject> projects = await AddProjectsAsync(3);

        bool removed = await _service.RemoveAsync(projects[0]);

        Assert.That(removed, Is.True);
        Assert.That(_service.Count, Is.EqualTo(2));
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _service.ReadAsync(projects[0].Id));
    }

    [Test]
    public async Task RemoveAsync_MissingElement_ReturnsFalse()
    {
        await AddProjectsAsync(1);

        Assert.That(await _service.RemoveAsync(NewProject()), Is.False);
        Assert.That(_service.Count, Is.EqualTo(1));
    }

    // ---------- IEnumerable<T> ----------

    [Test]
    public async Task Enumeration_IteratesElementsInOrder()
    {
        await AddProjectsAsync(3);

        Assert.That(_service.Select(p => p.Title), Is.EqualTo(new[] { "P0", "P1", "P2" }));
    }

    [Test]
    public async Task Enumeration_ModifyingServiceDuringForeach_DoesNotThrow()
    {
        await AddProjectsAsync(5);

        Assert.DoesNotThrowAsync(async () =>
        {
            foreach (GameProject _ in _service)
            {
                await _service.CreateAsync(NewProject());
            }
        });
        Assert.That(_service.Count, Is.EqualTo(10));
    }

    // ---------- потокобезпечність ----------

    [Test]
    public async Task CreateAsync_FromManyThreadsInParallel_StoresAllElements()
    {
        const int count = 1000;
        var tasks = new Task<bool>[count];

        Parallel.For(0, count, i => tasks[i] = _service.CreateAsync(GameProject.CreateNew()));
        bool[] results = await Task.WhenAll(tasks);

        Assert.That(results, Is.All.True);
        Assert.That(_service.Count, Is.EqualTo(count));
        Assert.That(_service.Select(p => p.Id).Distinct().Count(), Is.EqualTo(count));
    }

    [Test]
    public async Task ConcurrentCreateAndRemove_KeepsCountConsistent()
    {
        List<GameProject> initial = await AddProjectsAsync(200);
        int created = 0, removed = 0;

        Task creator = Task.Run(async () =>
        {
            for (int i = 0; i < 300; i++)
            {
                if (await _service.CreateAsync(GameProject.CreateNew()))
                {
                    Interlocked.Increment(ref created);
                }
            }
        });
        Task remover = Task.Run(async () =>
        {
            foreach (GameProject project in initial.Take(150))
            {
                if (await _service.RemoveAsync(project))
                {
                    Interlocked.Increment(ref removed);
                }
            }
        });
        await Task.WhenAll(creator, remover);

        Assert.That(created, Is.EqualTo(300));
        Assert.That(removed, Is.EqualTo(150));
        Assert.That(_service.Count, Is.EqualTo(200 + 300 - 150));
    }

    [Test]
    public async Task UpdateAsync_ConcurrentUpdatesOfSameElement_AllSucceed()
    {
        GameProject project = NewProject("Shared");
        await _service.CreateAsync(project);
        var updates = new ConcurrentBag<Task<bool>>();

        Parallel.For(0, 200, i =>
        {
            var replacement = new GameProject($"Version {i}", "Puzzle", "Godot", 1000m) { Id = project.Id };
            updates.Add(_service.UpdateAsync(replacement));
        });
        bool[] results = await Task.WhenAll(updates);

        Assert.That(results, Has.Length.EqualTo(200).And.All.True);
        Assert.That(_service.Count, Is.EqualTo(1));
        Assert.That((await _service.ReadAsync(project.Id)).Title, Does.StartWith("Version "));
    }

    // ---------- SaveAsync / LoadAsync ----------

    [Test]
    public async Task SaveAsync_ThenLoadAsync_RestoresElementsInSameOrder()
    {
        List<GameProject> projects = await AddProjectsAsync(5);
        projects[2].Spend(250m);
        projects[2].ChangeStatus(ProjectStatus.Production);

        bool saved = await _service.SaveAsync();
        var loaded = new CrudServiceAsync<GameProject>(_filePath);
        bool loadedOk = await loaded.LoadAsync();

        Assert.That(saved, Is.True);
        Assert.That(File.Exists(_filePath), Is.True);
        Assert.That(loadedOk, Is.True);
        Assert.That(loaded.Select(p => p.Id), Is.EqualTo(projects.Select(p => p.Id)));
        GameProject restored = await loaded.ReadAsync(projects[2].Id);
        Assert.That(restored.Spent, Is.EqualTo(250m));
        Assert.That(restored.Status, Is.EqualTo(ProjectStatus.Production));
    }

    [Test]
    public async Task SaveAsync_ThenLoadAsync_PreservesDerivedEmployeeTypes()
    {
        string employeesPath = Path.Combine(Path.GetTempPath(), $"gamestudio-employees-{Guid.NewGuid():N}.json");
        try
        {
            var employees = new CrudServiceAsync<Employee>(employeesPath);
            await employees.CreateAsync(Programmer.CreateNew());
            await employees.CreateAsync(Artist.CreateNew());

            await employees.SaveAsync();
            var loaded = new CrudServiceAsync<Employee>(employeesPath);
            await loaded.LoadAsync();

            Assert.That(loaded.Select(e => e.GetType()), Is.EqualTo(new[] { typeof(Programmer), typeof(Artist) }));
            Assert.That(loaded.Sum(e => e.CalculateMonthlySalary()), Is.EqualTo(employees.Sum(e => e.CalculateMonthlySalary())));
        }
        finally
        {
            File.Delete(employeesPath);
        }
    }

    [Test]
    public async Task SaveAsync_CalledConcurrently_AllSucceedAndFileIsValid()
    {
        await AddProjectsAsync(300);

        bool[] results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => _service.SaveAsync())));
        var loaded = new CrudServiceAsync<GameProject>(_filePath);

        Assert.That(results, Is.All.True);
        Assert.That(await loaded.LoadAsync(), Is.True);
        Assert.That(loaded.Count, Is.EqualTo(300));
    }

    [Test]
    public async Task LoadAsync_MissingFile_ReturnsFalse()
    {
        Assert.That(await _service.LoadAsync(), Is.False);
    }

    [Test]
    public void Constructor_EmptyFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new CrudServiceAsync<GameProject>(" "));
    }

    // ---------- допоміжні методи ----------

    private static GameProject NewProject(string title = "Test project") => new(title, "Puzzle", "Godot", 1000m);

    private async Task<List<GameProject>> AddProjectsAsync(int count)
    {
        var projects = new List<GameProject>();
        for (int i = 0; i < count; i++)
        {
            GameProject project = NewProject($"P{i}");
            await _service.CreateAsync(project);
            projects.Add(project);
        }

        return projects;
    }
}
