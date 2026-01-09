using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Commands;
using JobVacancyCollector.Infrastructure.Parsers;
using MassTransit;

namespace JobVacancyCollector.Worker
{
    public class VacancyBackgroundJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VacancyBackgroundJob> _logger;
        private readonly IScrapingControlService _controlService;

        public VacancyBackgroundJob(IServiceProvider serviceProvider, ILogger<VacancyBackgroundJob> logger, IScrapingControlService controlService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _controlService = controlService;
        }
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                _logger.LogInformation("Starting a scheduled scan of all sites.");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                    var scraperFactory = scope.ServiceProvider.GetRequiredService<IScraperFactory>();
                    var sites = scraperFactory.GetRegisterNames();

                    foreach (var site in sites)
                    {
                        _logger.LogInformation($"We send a scraping command for: {site}");

                        await publishEndpoint.Publish(new StartScrapingCommand
                        {
                            Site = site,
                        }, ct);
                    }
                }

                _logger.LogInformation("All commands sent. Next check in 4 hours.");

                await Task.Delay(TimeSpan.FromHours(4), ct);
            }
        }
    }
}
