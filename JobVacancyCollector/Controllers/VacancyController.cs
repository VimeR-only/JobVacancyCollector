using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Models;
using JobVacancyCollector.Infrastructure.Parsers;
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

        public VacancyController(IVacancyService vacancyService)
        {
            _vacancyService = vacancyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vacancys = await _vacancyService.GetAllAsync();

            if (vacancys == null) return NotFound();

            return Ok(vacancys);
        }

        [HttpPost("scrape")]
        public IActionResult Scrape(ScraperSource source, string? city, int? maxPage)
        {
            var scopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

            _ = Task.Run(async () =>
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var scopedService = scope.ServiceProvider.GetRequiredService<IVacancyService>();

                    try
                    {
                        await scopedService.ScrapeAndSaveAsync(source.ToString(), city, maxPage, CancellationToken.None);
                        
                        Console.WriteLine($"Parsing {source} successfully completed.");
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<VacancyController>>();
                        logger.LogError(ex, "Parsing error");
                    }
                }
            });

            return Accepted(new { message = "Parsing is running in a separate thread" });
        }

        [HttpPost("scrape-multiple")]
        public IActionResult ScrapeMultiple([FromQuery] List<ScraperSource> sources, string? city, int? maxPage)
        {
            var scopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

            foreach (var source in sources)
            {
                var siteName = source.ToString();

                _ = Task.Run(async () =>
                {
                    using var scope = scopeFactory.CreateScope();

                    var scopedService = scope.ServiceProvider.GetRequiredService<IVacancyService>();

                    try
                    {
                        await scopedService.ScrapeAndSaveAsync(siteName, city, maxPage, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<VacancyController>>();
                        logger.LogError(ex, "Parsing error {Site}", siteName);
                    }
                });
            }

            return Accepted(new { message = $"Parallel parsing started for: {string.Join(", ", sources)}" });
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
