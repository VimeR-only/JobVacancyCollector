using DocumentFormat.OpenXml.Bibliography;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Infrastructure.Parsers.Dou;
using Microsoft.Extensions.DependencyInjection;

namespace JobVacancyCollector.Infrastructure.Parsers
{
    public class ScraperFactory : IScraperFactory
    {
        private readonly IEnumerable<IVacancyScraper> _scrapers;

        public ScraperFactory(IEnumerable<IVacancyScraper> scrapers)
        {
            _scrapers = scrapers;
        }

        public IVacancyScraper GetScraper(string source)
        {
            var normalizedSource = source.ToLower().Replace("jobs.", "").Replace(".ua", "");

            return normalizedSource switch
            {
                "dou" => _scrapers.First(s => s.SourceName == "jobs.dou.ua"),
                "work" => _scrapers.First(s => s.SourceName == "Work.ua"),
                _ => throw new ArgumentException($"Сайт {source} не підтримується")
            };
        }
    }
}