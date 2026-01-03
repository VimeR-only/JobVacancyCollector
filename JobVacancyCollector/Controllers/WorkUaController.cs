using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Models;
using JobVacancyCollector.Infrastructure.Parsers;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace JobVacancyCollector.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VacancyController : ControllerBase
    {
        private readonly IVacancyService _vacancyService;
        private readonly IScraperFactory _scraperFactory;

        public VacancyController(IVacancyService vacancyService, IScraperFactory scraperFactory)
        {
            _vacancyService = vacancyService;
            _scraperFactory = scraperFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vacancys = await _vacancyService.GetAllAsync();

            if (vacancys == null) return NotFound();

            return Ok(vacancys);
        }

        [HttpPost("scrape")]
        public IActionResult ScrapeVacancy([FromQuery] ScraperSource source, string? city, int? maxPage)
        {
            try
            {
                var siteName = source.ToString();
                _vacancyService.ScrapeAndSaveAsync(siteName, city, maxPage, CancellationToken.None);

                return Accepted(new { message = "Parsing process is running in the background" });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
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
