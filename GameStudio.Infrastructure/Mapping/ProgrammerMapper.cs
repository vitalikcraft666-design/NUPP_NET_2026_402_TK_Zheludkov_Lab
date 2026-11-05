using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Infrastructure.Models;

namespace GameStudio.Infrastructure.Mapping;

/// <summary>
/// ЛР4: перетворення Programmer ↔ ProgrammerModel. Потрібне для окремого CRUD-сервісу програмістів
/// (ICrudServiceAsync&lt;Programmer&gt;), який працює лише з таблицею Programmers (TPT).
/// Саме копіювання полів виконує вже наявний <see cref="EmployeeMapper"/>.
/// </summary>
public class ProgrammerMapper : IModelMapper<Programmer, ProgrammerModel>
{
    private readonly EmployeeMapper _employeeMapper = new();

    public Programmer ToDomain(ProgrammerModel model) => (Programmer)_employeeMapper.ToDomain(model);

    public async Task<ProgrammerModel> ToModelAsync(Programmer domain) =>
        (ProgrammerModel)await _employeeMapper.ToModelAsync(domain).ConfigureAwait(false);

    public Task UpdateModelAsync(ProgrammerModel model, Programmer domain) => _employeeMapper.UpdateModelAsync(model, domain);
}
