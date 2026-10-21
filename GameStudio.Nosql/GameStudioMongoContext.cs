using GameStudio.Nosql.Documents;
using GameStudio.Nosql.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace GameStudio.Nosql;

/// <summary>
/// Підключення до MongoDB: клієнт, база даних gamestudio та її колекції (employees, projects).
/// Рядок підключення береться зі змінної середовища GAMESTUDIO_MONGO або за замовчуванням mongodb://localhost:27017.
/// </summary>
public class GameStudioMongoContext
{
    public const string DefaultConnectionString = "mongodb://localhost:27017";
    public const string DefaultDatabaseName = "gamestudio";
    public const string EmployeesCollectionName = "employees";
    public const string ProjectsCollectionName = "projects";

    // статичний конструктор — один раз налаштовує, як .NET-типи записуються в BSON
    static GameStudioMongoContext()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
    }

    public GameStudioMongoContext(string connectionString, string databaseName = DefaultDatabaseName)
    {
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

        Client = new MongoClient(settings);
        DatabaseName = databaseName;
        Database = Client.GetDatabase(databaseName);
    }

    public IMongoClient Client { get; }

    public IMongoDatabase Database { get; }

    public string DatabaseName { get; }

    public IMongoCollection<EmployeeDocument> Employees => Database.GetCollection<EmployeeDocument>(EmployeesCollectionName);

    public IMongoCollection<GameProjectDocument> Projects => Database.GetCollection<GameProjectDocument>(ProjectsCollectionName);

    // статичний метод — рядок підключення зі змінної середовища або значення за замовчуванням
    public static string GetConnectionString() =>
        Environment.GetEnvironmentVariable("GAMESTUDIO_MONGO") ?? DefaultConnectionString;

    public MongoRepository<EmployeeDocument> CreateEmployeeRepository() => new(Database, EmployeesCollectionName);

    public MongoRepository<GameProjectDocument> CreateProjectRepository() => new(Database, ProjectsCollectionName);

    // метод — перевірка з'єднання (команда ping) і версія сервера
    public async Task<string> GetServerVersionAsync()
    {
        await Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        BsonDocument buildInfo = await Database.RunCommandAsync<BsonDocument>(new BsonDocument("buildInfo", 1));
        return buildInfo["version"].AsString;
    }

    // метод — видаляє базу даних (для повторного заповнення)
    public Task DropDatabaseAsync() => Client.DropDatabaseAsync(DatabaseName);

    // метод — індекси: унікальний PublicId, статус проєкту, критичність вкладених багів
    public async Task CreateIndexesAsync()
    {
        await Employees.Indexes.CreateOneAsync(new CreateIndexModel<EmployeeDocument>(
            Builders<EmployeeDocument>.IndexKeys.Ascending(e => e.PublicId), new CreateIndexOptions { Unique = true }));

        await Projects.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<GameProjectDocument>(
                Builders<GameProjectDocument>.IndexKeys.Ascending(p => p.PublicId), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<GameProjectDocument>(Builders<GameProjectDocument>.IndexKeys.Ascending(p => p.Status)),
            new CreateIndexModel<GameProjectDocument>(Builders<GameProjectDocument>.IndexKeys.Ascending("Bugs.Severity"))
        });
    }
}
