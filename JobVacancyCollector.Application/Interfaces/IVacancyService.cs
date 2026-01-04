using JobVacancyCollector.Domain.Models;

namespace JobVacancyCollector.Application.Interfaces
{
    public interface IVacancyService
    {
        Task ScrapeAndSaveAsync(string site, string? city, int? maxPage, CancellationToken ct);
        Task<bool> ScrapeNewAsync(string site, string? city, int? maxPage, CancellationToken ct = default);
        Task<bool> ScrapeNotExistentDeleteAsync(string site, string? city, int? maxPage, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vacancy>> GetAllAsync(string? site);
        Task<Vacancy?> GetByIdAsync(string site, string id);
        Task ClearDb();
        Task<MemoryStream> ExportExcel(); //Temporarily added for github users.I don't need it, but it will be made better later.
    }
}
