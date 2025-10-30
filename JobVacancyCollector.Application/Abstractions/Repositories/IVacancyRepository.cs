using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Application.Abstractions.Repositories
{
    public interface IVacancyRepository
    {
        Task AddRangeAsync(IEnumerable<Vacancy> vacancies);
        Task ClearAsync();
        Task<bool> ExistsAsync(string? sourceId = null, string? sourceName = null);
        Task<IEnumerable<string>> GetAllIdsAsync();
    }
}
