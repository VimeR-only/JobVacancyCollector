using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Application.Interfaces
{
    public interface IVacancyService
    {
        Task<bool> ScrapeAndSaveAsync(string? city, int? maxPage, CancellationToken cancellationToken = default);
        Task<bool> ScrapeNewAndSaveAsync(string? city, int? maxPage, CancellationToken cancellationToken = default);
        Task<bool> ScrapeNotExistentDeleteAsync(string? city, int? maxPage, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vacancy>> GetAllAsync();
        Task<Vacancy?> GetByIdAsync(string id);
        Task ClearDb();
    }
}
