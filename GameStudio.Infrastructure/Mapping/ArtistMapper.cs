using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Infrastructure.Models;

namespace GameStudio.Infrastructure.Mapping;

/// <summary>
/// ЛР4: перетворення Artist ↔ ArtistModel для окремого CRUD-сервісу художників
/// (ICrudServiceAsync&lt;Artist&gt;, таблиця Artists). Копіювання полів виконує <see cref="EmployeeMapper"/>.
/// </summary>
public class ArtistMapper : IModelMapper<Artist, ArtistModel>
{
    private readonly EmployeeMapper _employeeMapper = new();

    public Artist ToDomain(ArtistModel model) => (Artist)_employeeMapper.ToDomain(model);

    public async Task<ArtistModel> ToModelAsync(Artist domain) =>
        (ArtistModel)await _employeeMapper.ToModelAsync(domain).ConfigureAwait(false);

    public Task UpdateModelAsync(ArtistModel model, Artist domain) => _employeeMapper.UpdateModelAsync(model, domain);
}
