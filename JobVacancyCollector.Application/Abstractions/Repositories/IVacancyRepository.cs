using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Application.Abstractions.Repositories
{
    public interface IVacancyRepository
    {
        Task<bool> AddRangeAsync(IEnumerable<Vacancy> vacancies);
        Task<bool> ClearAsync();
        Task<bool> ExistsAsync(string? sourceId = null, string? sourceName = null);
        Task<IEnumerable<string>> GetAllIdsAsync();
        Task<bool> RemoveIdAsync(string sourceId);
        Task<bool> RemoveRangeAsync(IEnumerable<string> sourceIds);
    }
}
