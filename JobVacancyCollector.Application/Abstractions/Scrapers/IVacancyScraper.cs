using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Application.Abstractions.Scrapers
{
    public interface IVacancyScraper
    {
        public string SourceName { get; set; }

        Task<IEnumerable<Vacancy>> ScrapeAsync(string? cityOrOption, int? maxPage = null, CancellationToken cancellationToken = default);
        Task<List<string>> ScraperUrlAsync(string? cityOrOption = "Вся Україна", int? maxPage = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vacancy>> ScrapeDetailsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    }
}
