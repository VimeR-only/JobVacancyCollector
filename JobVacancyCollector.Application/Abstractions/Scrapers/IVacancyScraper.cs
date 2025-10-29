using JobVacancyCollector.Domain.Models;

namespace JobVacancyCollector.Application.Abstractions.Scrapers
{
    public interface IVacancyScraper
    {
        public string SourceName { get; set; }

        Task<IEnumerable<Vacancy>> ScrapeAsync(CancellationToken cancellationToken = default);
    }
}
