using GameStudio.Common.Interfaces;
using GameStudio.Common.Models;
using GameStudio.Infrastructure.Models;

namespace GameStudio.Infrastructure.Mapping;

/// <summary>Перетворення Employee (Programmer / Artist) ↔ EmployeeModel (ProgrammerModel / ArtistModel).</summary>
public class EmployeeMapper : IModelMapper<Employee, EmployeeModel>
{
    public Employee ToDomain(EmployeeModel model)
    {
        return model switch
        {
            ProgrammerModel p => new Programmer(p.FullName, p.BaseSalary, p.PrimaryLanguage, p.Grade, p.HireDate)
            {
                Id = p.PublicId,
                CommitsCount = p.CommitsCount
            },
            ArtistModel a => new Artist(a.FullName, a.BaseSalary, a.Specialization, a.MainTool, a.HireDate)
            {
                Id = a.PublicId,
                ArtworksCompleted = a.ArtworksCompleted
            },
            _ => throw new NotSupportedException($"Невідомий тип працівника: {model.GetType().Name}")
        };
    }

    public Task<EmployeeModel> ToModelAsync(Employee domain)
    {
        EmployeeModel model = domain switch
        {
            Programmer => new ProgrammerModel(),
            Artist => new ArtistModel(),
            _ => throw new NotSupportedException($"Невідомий тип працівника: {domain.GetType().Name}")
        };

        model.PublicId = domain.Id;
        Copy(domain, model);
        return Task.FromResult(model);
    }

    public Task UpdateModelAsync(EmployeeModel model, Employee domain)
    {
        Copy(domain, model);
        return Task.CompletedTask;
    }

    private static void Copy(Employee domain, EmployeeModel model)
    {
        model.FullName = domain.FullName;
        model.HireDate = domain.HireDate;
        model.BaseSalary = domain.BaseSalary;

        switch (domain, model)
        {
            case (Programmer p, ProgrammerModel pm):
                pm.PrimaryLanguage = p.PrimaryLanguage;
                pm.Grade = p.Grade;
                pm.CommitsCount = p.CommitsCount;
                break;
            case (Artist a, ArtistModel am):
                am.Specialization = a.Specialization;
                am.MainTool = a.MainTool;
                am.ArtworksCompleted = a.ArtworksCompleted;
                break;
            default:
                throw new InvalidOperationException("Тип працівника не можна змінити (Programmer ↔ Artist).");
        }
    }
}
