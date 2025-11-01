using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Application.Abstractions.Scrapers
{
    public interface IVacancyScraper
    {
        public string SourceName { get; set; }

        Task<IEnumerable<Vacancy>> ScrapeAsync(string? cityOrOption, int? maxPage = null, CancellationToken cancellationToken = default, IProgress<int>? progress = null);
        Task<IEnumerable<Vacancy>> ScrapeNewVacanciesAsync(string? cityOrOption = "Вся Україна", int? maxPage = 1, CancellationToken cancellationToken = default);
        Task<List<string>> ScrapeNotExistentVacanciesAsync(string? cityOrOption = "Вся Україна", int? maxPage = 1, CancellationToken cancellationToken = default);
    }
}
