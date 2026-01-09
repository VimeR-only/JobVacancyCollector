namespace JobVacancyCollector.Application.Interfaces
{
    public interface IScrapingControlService
    {
        void SetStop(string siteName);
        void SetStart(string siteName);
        CancellationToken GetLinkedToken(string siteName, CancellationToken externalToken);
    }
}
