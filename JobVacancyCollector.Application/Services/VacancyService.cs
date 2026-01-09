using ClosedXML.Excel;
using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Models;
using Microsoft.Extensions.Logging;

namespace JobVacancyCollector.Application.Services
{

    public class VacancyService : IVacancyService
    {
        private readonly IVacancyRepository _vacancyRepository;
        private readonly IScraperFactory _scraperFactory;
        private readonly ILogger<VacancyService> _logger;
        private readonly IScrapingControlService _controlService;

        public VacancyService(IVacancyRepository vacancyRepository, IScraperFactory scraperFactory, ILogger<VacancyService> logger, IScrapingControlService controlService)
        {
            _vacancyRepository = vacancyRepository;
            _scraperFactory = scraperFactory;
            _logger = logger;
            _controlService = controlService;
        }

        public async Task ScrapeAndSaveAsync(string site, string? city, int? maxPage, CancellationToken ct)
        {
            var scraper = _scraperFactory.GetScraper(site);

            var linkedToken = _controlService.GetLinkedToken(scraper.SourceName, ct);

            var batch = new List<Vacancy>();
            const int batchSize = 50;

            await foreach (var vacancy in scraper.ScrapeAsync(city, maxPage, linkedToken))
            {

                if (await _vacancyRepository.ExistsAsync(scraper.SourceName, vacancy.SourceId)) continue;

                batch.Add(vacancy);

                if (batch.Count >= batchSize)
                {
                    await _vacancyRepository.AddRangeAsync(batch);

                    _logger.LogInformation($"Saved a pack of {batch.Count} vacancies");

                    batch.Clear();
                }
            }

            if (batch.Any()) await _vacancyRepository.AddRangeAsync(batch);
        }

        public async Task<bool> ScrapeNewAsync(string site, string? city, int? maxPage, CancellationToken ct = default)
        {
            var scraper = _scraperFactory.GetScraper(site);

            var linkedToken = _controlService.GetLinkedToken(scraper.SourceName, ct);

            var batch = new List<Vacancy>();
            const int batchSize = 50;

            await foreach (var vacancy in scraper.ScrapeAsync(city, maxPage, linkedToken))
            {
                if (await _vacancyRepository.ExistsAsync(scraper.SourceName, vacancy.SourceId)) continue;

                batch.Add(vacancy);

                if (batch.Count >= batchSize)
                {
                    await _vacancyRepository.AddRangeAsync(batch);

                    _logger.LogInformation($"Saved a pack of {batch.Count} vacancies for {site}");

                    batch.Clear();
                }
            }

            if (batch.Any()) await _vacancyRepository.AddRangeAsync(batch);

            return true;
        }

        public async Task<bool> ScrapeNotExistentDeleteAsync(string site, string? city, int? maxPage, CancellationToken ct = default)
        {
            var scraper = _scraperFactory.GetScraper(site);
            var currentIds = new HashSet<string>();

            var linkedToken = _controlService.GetLinkedToken(scraper.SourceName, ct);

            await foreach (var url in scraper.ScraperUrlAsync(city, maxPage, linkedToken).WithCancellation(linkedToken))
            {
                currentIds.Add(scraper.GetIdFromUrl(url));
            }

            var dbIds = (await _vacancyRepository.GetAllIdsAsync(scraper.SourceName)).ToHashSet();
            var idsToRemove = dbIds.Where(id => !currentIds.Contains(id)).ToList();

            if (!idsToRemove.Any()) return false;

            _logger.LogWarning($"Removing {idsToRemove.Count} outdated vacancies for {scraper.SourceName}");

            return await _vacancyRepository.RemoveRangeAsync(scraper.SourceName, idsToRemove);
        }

        public async Task<IEnumerable<Vacancy>> GetAllAsync(string? site)
        {
            if (site == null)
            {
                return await _vacancyRepository.GetAllAsync();
            }

            return await _vacancyRepository.GetAllAsync(_scraperFactory.GetSourceNameScraper(site));
        }

        public async Task<Vacancy?> GetByIdAsync(string site, string id)
        {
            return await _vacancyRepository.GetByIdAsync(_scraperFactory.GetSourceNameScraper(site), id);
        }

        public async Task ClearDb()
        {
            await _vacancyRepository.ClearAsync();
        }

        public async Task<MemoryStream> ExportExcel() //Temporarily added for github users.I don't need it, but it will be made better later.
        {
            var allVacancys = await _vacancyRepository.GetAllAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Vacancies");

            var stream = new MemoryStream();

            ws.Cell(1, 1).Value = "SourceId";
            ws.Cell(1, 2).Value = "SourceName";
            ws.Cell(1, 3).Value = "Title";
            ws.Cell(1, 4).Value = "Salary";
            ws.Cell(1, 5).Value = "SalaryNote";
            ws.Cell(1, 6).Value = "Company";
            ws.Cell(1, 7).Value = "CompanyNote";
            ws.Cell(1, 8).Value = "City";
            ws.Cell(1, 9).Value = "Location";
            ws.Cell(1, 10).Value = "Phone";
            ws.Cell(1, 11).Value = "NameContact";
            ws.Cell(1, 12).Value = "Conditions";
            ws.Cell(1, 13).Value = "Language";
            ws.Cell(1, 14).Value = "Description";

            for (int i = 0; i < allVacancys.Count(); i++)
            {
                var v = allVacancys.ElementAt(i);

                ws.Cell(i + 2, 1).Value = v.SourceId;
                ws.Cell(i + 2, 2).Value = v.SourceName;
                ws.Cell(i + 2, 3).Value = v.Title;
                ws.Cell(i + 2, 4).Value = v.Salary;
                ws.Cell(i + 2, 5).Value = v.SalaryNote;
                ws.Cell(i + 2, 6).Value = v.Company;
                ws.Cell(i + 2, 7).Value = v.CompanyNote;
                ws.Cell(i + 2, 8).Value = v.City;
                ws.Cell(i + 2, 9).Value = v.Location;
                ws.Cell(i + 2, 10).Value = v.Phone;
                ws.Cell(i + 2, 11).Value = v.NameContact;
                ws.Cell(i + 2, 12).Value = v.Conditions;
                ws.Cell(i + 2, 13).Value = v.Language;
                ws.Cell(i + 2, 14).Value = v.Description;
            }
            workbook.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }
    }
}