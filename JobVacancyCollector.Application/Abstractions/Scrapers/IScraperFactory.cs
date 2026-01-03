namespace JobVacancyCollector.Application.Abstractions.Scrapers
{
    public interface IScraperFactory
    {
        IVacancyScraper GetScraper(string source);
    }
}