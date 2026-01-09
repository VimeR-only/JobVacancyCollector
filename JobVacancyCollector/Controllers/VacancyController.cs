using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Commands;
using JobVacancyCollector.Domain.Models;
using JobVacancyCollector.Infrastructure.Parsers;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;

namespace JobVacancyCollector.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VacancyController : ControllerBase
    {
        private readonly IVacancyService _vacancyService;
        private readonly IPublishEndpoint _publishEndpoint;

        public VacancyController(IVacancyService vacancyService, IPublishEndpoint publishEndpoint)
        {
            _vacancyService = vacancyService;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(ScraperSource source)
        {
            var vacancys = await _vacancyService.GetAllAsync(source.ToString());

            if (vacancys == null) return NotFound();

            return Ok(vacancys);
        }

        [HttpPost("scrape")]
        public async Task<IActionResult> Scrape(ScraperSource source, string? city, int? maxPage)
        {
            await _publishEndpoint.Publish(new StartScrapingCommand
            {
                Site = source.ToString(),
                City = city,
                MaxPages = maxPage ?? 5
            });

            return Accepted(new { message = $"Task for {source} added to RabbitMQ queue" });
        }

        [HttpPost("scrape-multiple")]
        public async Task<IActionResult> ScrapeMultiple([FromQuery] List<ScraperSource> sources, string? city, int? maxPage)
        {
            foreach (var source in sources)
            {
                await _publishEndpoint.Publish(new StartScrapingCommand
                {
                    Site = source.ToString(),
                    City = city,
                    MaxPages = maxPage ?? 5
                });
            }

            return Accepted(new { message = $"Commands for {sources.Count} sites queued" });
        }

        [HttpPost("stop")]
        public async Task<IActionResult> ScrapeStop([FromQuery] List<ScraperSource> sources)
        {
            foreach (var source in sources)
            {
                await _publishEndpoint.Publish(new StopScrapingCommand
                {
                    Site = source.ToString(),
                });
            }

            return Accepted(new { message = $"Command to stop parsing {sources.Count} queued sites" });
        }

        [HttpPost("clear-db")]
        public async Task<ActionResult> ClearDb()
        {
            await _vacancyService.ClearDb();
            
            return Ok();
        }

        [HttpGet("export")]
        public async Task<ActionResult> Export() //Temporarily added for github users.I don't need it, but it will be made better later.
        {
            var stream = await _vacancyService.ExportExcel();
            
            return File(stream,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "vacancies.xlsx");
        }
    }
}
