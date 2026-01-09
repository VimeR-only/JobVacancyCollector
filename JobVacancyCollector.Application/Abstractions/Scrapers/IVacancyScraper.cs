using JobVacancyCollector.Domain.Models;
using System.Runtime.CompilerServices;

namespace JobVacancyCollector.Application.Abstractions.Scrapers
{
    public interface IVacancyScraper
    {
        public string SourceName { get; set; }
        string GetIdFromUrl(string url);
        IAsyncEnumerable<Vacancy> ScrapeAsync(string? cityOrOption, int? maxPage = null, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        IAsyncEnumerable<Vacancy> ScrapeDetailsAsync(IAsyncEnumerable<string> urls, [EnumeratorCancellation] CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> ScraperUrlAsync(string? cityOrOption = "Вся Україна", int? maxPage = null, CancellationToken cancellationToken = default);
    }
}
