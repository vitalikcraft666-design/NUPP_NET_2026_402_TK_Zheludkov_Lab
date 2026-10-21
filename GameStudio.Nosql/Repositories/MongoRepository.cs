using System.Linq.Expressions;
using GameStudio.Common.Interfaces;
using GameStudio.Nosql.Documents;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GameStudio.Nosql.Repositories;

/// <summary>
/// Додаткове завдання ЛР3: реалізація того самого інтерфейсу <see cref="IRepository{T}"/>
/// для нереляційної документної БД MongoDB (без Entity Framework, через офіційний MongoDB.Driver).
/// На відміну від EF Core, кожна операція одразу виконується на сервері (немає SaveChanges).
/// </summary>
public class MongoRepository<T> : IRepository<T>
    where T : class, IDocument
{
    private const string CountersCollectionName = "counters";

    private readonly IMongoCollection<T> _collection;
    private readonly IMongoCollection<BsonDocument> _counters;
    private readonly string _collectionName;

    // конструктор — база даних і назва колекції
    public MongoRepository(IMongoDatabase database, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        _collectionName = collectionName;
        _collection = database.GetCollection<T>(collectionName);
        _counters = database.GetCollection<BsonDocument>(CountersCollectionName);
    }

    public IMongoCollection<T> Collection => _collection;

    // метод — документ за _id або KeyNotFoundException
    public async Task<T> GetByIdAsync(int id)
    {
        return await _collection.Find(document => document.Id == id).FirstOrDefaultAsync()
               ?? throw new KeyNotFoundException($"Документ {typeof(T).Name} з _id {id} не знайдено в колекції {_collectionName}.");
    }

    // метод — усі документи колекції, упорядковані за _id
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _collection.Find(FilterDefinition<T>.Empty).SortBy(document => document.Id).ToListAsync();
    }

    // метод — вставляє документ (insertOne); якщо Id = 0, видає наступний номер з лічильника
    public async Task AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id == 0)
        {
            entity.Id = await NextIdAsync();
        }

        await _collection.InsertOneAsync(entity);
    }

    // метод — повністю замінює документ з тим самим _id (replaceOne)
    public async Task Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ReplaceOneResult result = await _collection.ReplaceOneAsync(document => document.Id == entity.Id, entity);
        if (result.MatchedCount == 0)
        {
            throw new KeyNotFoundException($"Документ з _id {entity.Id} не знайдено в колекції {_collectionName}.");
        }
    }

    // метод — видаляє документ (deleteOne)
    public async Task Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _collection.DeleteOneAsync(document => document.Id == entity.Id);
    }

    // додатковий метод — пошук за умовою (умова перетворюється на фільтр MongoDB)
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter)
    {
        return await _collection.Find(filter).SortBy(document => document.Id).ToListAsync();
    }

    public Task<long> CountAsync() => _collection.CountDocumentsAsync(FilterDefinition<T>.Empty);

    // аналог AUTOINCREMENT: атомарний лічильник у колекції counters ($max + $inc у findOneAndUpdate)
    private async Task<int> NextIdAsync()
    {
        T? last = await _collection.Find(FilterDefinition<T>.Empty).SortByDescending(document => document.Id).Limit(1).FirstOrDefaultAsync();
        FilterDefinition<BsonDocument> counterFilter = Builders<BsonDocument>.Filter.Eq("_id", _collectionName);

        // лічильник не може бути меншим за найбільший уже наявний _id
        await _counters.UpdateOneAsync(counterFilter,
            Builders<BsonDocument>.Update.Max("value", last?.Id ?? 0),
            new UpdateOptions { IsUpsert = true });

        BsonDocument counter = await _counters.FindOneAndUpdateAsync(counterFilter,
            Builders<BsonDocument>.Update.Inc("value", 1),
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After });

        return counter["value"].AsInt32;
    }
}
