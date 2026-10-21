using GameStudio.Common.Interfaces;

namespace GameStudio.Common.Services;

/// <summary>
/// ЛР3: оновлена асинхронна версія узагальненого CRUD-сервісу, яка працює з даними через репозиторій.
/// <list type="bullet">
/// <item><typeparamref name="TDomain"/> — доменна модель (Employee, GameProject, BugReport) з Guid-ідентифікатором;</item>
/// <item><typeparamref name="TModel"/> — модель бази даних (EmployeeModel, ...), з якою працює репозиторій;</item>
/// <item>перетворення між ними виконує <see cref="IModelMapper{TDomain,TModel}"/>.</item>
/// </list>
/// Create / Update / Remove лише реєструють зміни в репозиторії, а <see cref="SaveAsync"/> зберігає їх у БД
/// однією транзакцією (шаблон «одиниця роботи»). DbContext не є потокобезпечним, тому всі звернення до
/// репозиторію виконуються по черзі через <see cref="SemaphoreSlim"/>, і сервіс можна викликати з різних потоків.
/// </summary>
public class CrudServiceAsync<TDomain, TModel> : ICrudServiceAsync<TDomain>
    where TDomain : class, IEntity
    where TModel : class
{
    private readonly IEntityRepository<TModel> _repository;
    private readonly IModelMapper<TDomain, TModel> _mapper;

    // семафор з одним дозволом: у кожен момент з репозиторієм працює лише один потік
    private readonly SemaphoreSlim _gate = new(1, 1);

    // конструктор — залежності (репозиторій і перетворювач) передаються ззовні
    public CrudServiceAsync(IEntityRepository<TModel> repository, IModelMapper<TDomain, TModel> mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>Текст помилки останнього невдалого <see cref="SaveAsync"/>.</summary>
    public string? LastSaveError { get; private set; }

    // метод — додає новий елемент; false, якщо елемент з таким Id уже є в БД
    public async Task<bool> CreateAsync(TDomain element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (element.Id == Guid.Empty)
        {
            element.Id = Guid.NewGuid();
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (await _repository.GetByPublicIdAsync(element.Id).ConfigureAwait(false) is not null)
            {
                return false;
            }

            TModel model = await _mapper.ToModelAsync(element).ConfigureAwait(false);
            await _repository.AddAsync(model).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    // метод — читає елемент за Id або кидає KeyNotFoundException
    public async Task<TDomain> ReadAsync(Guid id)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            TModel model = await _repository.GetByPublicIdAsync(id).ConfigureAwait(false)
                           ?? throw new KeyNotFoundException($"Елемент з Id {id} не знайдено.");
            return _mapper.ToDomain(model);
        }
        finally
        {
            _gate.Release();
        }
    }

    // метод — усі елементи з БД
    public async Task<IEnumerable<TDomain>> ReadAllAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            IEnumerable<TModel> models = await _repository.GetAllAsync().ConfigureAwait(false);
            return models.Select(_mapper.ToDomain).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    // метод — пагінація (сторінки з 1); Skip/Take виконуються на стороні БД
    public async Task<IEnumerable<TDomain>> ReadAllAsync(int page, int amount)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Номер сторінки має бути не менше 1.");
        }

        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Розмір сторінки має бути не менше 1.");
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            IEnumerable<TModel> models = await _repository.GetPageAsync(page, amount).ConfigureAwait(false);
            return models.Select(_mapper.ToDomain).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    // метод — оновлює існуючий запис; false, якщо запису немає
    public async Task<bool> UpdateAsync(TDomain element)
    {
        ArgumentNullException.ThrowIfNull(element);

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            TModel? model = await _repository.GetByPublicIdAsync(element.Id).ConfigureAwait(false);
            if (model is null)
            {
                return false;
            }

            await _mapper.UpdateModelAsync(model, element).ConfigureAwait(false);
            await _repository.Update(model).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    // метод — видаляє запис; false, якщо запису немає
    public async Task<bool> RemoveAsync(TDomain element)
    {
        ArgumentNullException.ThrowIfNull(element);

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            TModel? model = await _repository.GetByPublicIdAsync(element.Id).ConfigureAwait(false);
            if (model is null)
            {
                return false;
            }

            await _repository.Delete(model).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    // метод — зберігає всі накопичені зміни в БД (одна транзакція SaveChanges)
    public async Task<bool> SaveAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _repository.SaveChangesAsync().ConfigureAwait(false);
            LastSaveError = null;
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LastSaveError = ex.GetBaseException().Message;
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    // додатковий метод — кількість записів у БД
    public async Task<int> CountAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await _repository.CountAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    // додатковий метод — кількість сторінок для заданого розміру
    public async Task<int> GetTotalPagesAsync(int amount)
    {
        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Розмір сторінки має бути не менше 1.");
        }

        int count = await CountAsync().ConfigureAwait(false);
        return (count + amount - 1) / amount;
    }
}
