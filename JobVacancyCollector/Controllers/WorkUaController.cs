using JobVacancyCollector.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost]
        public async Task<IActionResult> ScrapeVacancy(string? city, int? maxPage)
        {
            var status = await _vacancyService.ScrapeAndSaveAsync(city, maxPage);

            if (!status) return NotFound();

            return Ok(status);
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
