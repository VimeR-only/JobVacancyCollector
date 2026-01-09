using JobVacancyCollector.Domain.Models;

namespace JobVacancyCollector.Application.Abstractions.Repositories
{
    public interface IVacancyRepository
    {
        Task AddRangeAsync(IEnumerable<Vacancy> vacancies);
        Task<bool> ClearAsync();
        Task<bool> ExistsAsync(string sourceName, string sourceId);
        Task<bool> RemoveIdAsync(string sourceName, string sourceId);
        Task<bool> RemoveRangeAsync(string sourceName, IEnumerable<string> sourceIds);
        Task<IEnumerable<Vacancy>> GetAllAsync(string? sourceName = null);
        Task<IEnumerable<string>> GetAllIdsAsync(string? sourceName = null);
        Task<Vacancy?> GetByIdAsync(string sourceName, string id);
        Task<bool> AnyAsync(string sourceName, CancellationToken ct);
    }
}
