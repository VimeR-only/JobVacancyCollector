using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Commands;
using MassTransit;

namespace JobVacancyCollector.Worker.Consumers
{
    public class StopScrapingConsumer : IConsumer<StopScrapingCommand>
    {
        private readonly IScrapingControlService _controlService;
        private readonly IScraperFactory _scraperFactory;

        public StopScrapingConsumer(IScrapingControlService controlService, IScraperFactory scraperFactory)
        {
            _controlService = controlService;
            _scraperFactory = scraperFactory;
        }

        public async Task Consume(ConsumeContext<StopScrapingCommand> context)
        {
            var sourceName = _scraperFactory.GetSourceNameScraper(context.Message.Site);

            _controlService.SetStop(sourceName);
        }
    }
}
