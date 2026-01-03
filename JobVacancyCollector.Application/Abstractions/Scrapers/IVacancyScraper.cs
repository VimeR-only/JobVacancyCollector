using JobVacancyCollector.Domain.Models.WorkUa;
using System.Runtime.CompilerServices;

namespace JobVacancyCollector.Application.Abstractions.Scrapers
{
    public interface IVacancyScraper
    {
        public string SourceName { get; set; }

        IAsyncEnumerable<Vacancy> ScrapeAsync(string? cityOrOption, int? maxPage = null, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        IAsyncEnumerable<Vacancy> ScrapeDetailsAsync(IEnumerable<string> urls, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        Task<List<string>> ScraperUrlAsync(string? cityOrOption = "Вся Україна", int? maxPage = null, CancellationToken cancellationToken = default);
    }
}
