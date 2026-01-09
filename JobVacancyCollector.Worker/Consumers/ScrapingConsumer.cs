using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Commands;
using MassTransit;

namespace JobVacancyCollector.Worker.Consumers
{
    public class ScrapingConsumer : IConsumer<StartScrapingCommand>
    {
        private readonly IVacancyService _vacancyService;
        private readonly ILogger<ScrapingConsumer> _logger;
        private readonly IVacancyRepository _repository;
        private readonly IScrapingControlService _controlService;
        private readonly IScraperFactory _scraperFactory;

        public ScrapingConsumer(IVacancyService vacancyService, ILogger<ScrapingConsumer> logger, IVacancyRepository repository, IScrapingControlService controlService, IScraperFactory scraperFactory)
        {
            _vacancyService = vacancyService;
            _logger = logger;
            _repository = repository;
            _controlService = controlService;
            _scraperFactory = scraperFactory;
        }

        public async Task Consume(ConsumeContext<StartScrapingCommand> context)
        {
            var data = context.Message;
            var ct = context.CancellationToken;
            string sourceName = _scraperFactory.GetSourceNameScraper(data.Site);

            _controlService.SetStart(sourceName);

            _logger.LogInformation("--- Site Analysis: {Site} ---", sourceName);

            try
            {
                bool hasData = await _repository.AnyAsync(sourceName, ct);

                if (!hasData)
                {
                    _logger.LogWarning("The database for {Site} is empty! Starting a FULL scan.", sourceName);
                    await _vacancyService.ScrapeAndSaveAsync(sourceName, data.City, data.MaxPages, ct);
                }
                else
                {
                    _logger.LogInformation("The database for {Site} contains data. We are looking for new vacancies only.", sourceName);
                    bool foundNew = await _vacancyService.ScrapeNewAsync(sourceName, data.City, data.MaxPages, ct);

                    if (foundNew)
                        _logger.LogInformation("New jobs for {Site} have been successfully added.", sourceName);
                    else
                        _logger.LogInformation("There are currently no fresh vacancies on {Site}.", sourceName);
                }

                _logger.LogInformation("Running a job relevancy check for {Site}", sourceName);

                await _vacancyService.ScrapeNotExistentDeleteAsync(sourceName, data.City, data.MaxPages, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Scraping for {Site} was gracefully cancelled by user.", sourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while scraping site {Site}", sourceName);

                throw;
            }
        }
    }
}
