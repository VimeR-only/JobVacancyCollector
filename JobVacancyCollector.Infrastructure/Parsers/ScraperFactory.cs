using JobVacancyCollector.Application.Abstractions.Scrapers;

namespace JobVacancyCollector.Infrastructure.Parsers
{
    public class ScraperFactory : IScraperFactory
    {
        private readonly IEnumerable<IVacancyScraper> _scrapers;

        public ScraperFactory(IEnumerable<IVacancyScraper> scrapers)
        {
            _scrapers = scrapers;
        }

        public string GetSourceNameScraper(string source)
        {
            var scraper = GetScraper(source);

            return scraper.SourceName;
        }

        public IVacancyScraper GetScraper(string source)
        {
            var scraper = _scrapers.FirstOrDefault(s =>
                s.SourceName.Contains(source, StringComparison.OrdinalIgnoreCase) ||
                source.Contains(s.SourceName, StringComparison.OrdinalIgnoreCase));

            if (scraper == null)
            {
                throw new ArgumentException($"Website '{source}' is not supported. " +
                    $"Available sources: {string.Join(", ", _scrapers.Select(s => s.SourceName))}");
            }

            return scraper;
        }
    }
}