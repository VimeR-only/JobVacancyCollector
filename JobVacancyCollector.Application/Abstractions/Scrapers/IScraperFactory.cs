namespace JobVacancyCollector.Application.Abstractions.Scrapers
{
    public interface IScraperFactory
    {
        IVacancyScraper GetScraper(string source);
        IEnumerable<string> GetRegisterNames();
        string GetSourceNameScraper(string source);
    }
}