using ClosedXML.Excel;
using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Application.Services
{
    public class VacancyService : IVacancyService
    {
        private readonly IVacancyRepository _vacancyRepository;
        private readonly IVacancyScraper _vacancyScraper;
        public VacancyService(IVacancyRepository vacancyRepository, IVacancyScraper vacancyScraper)
        {
            _vacancyRepository = vacancyRepository;
            _vacancyScraper = vacancyScraper;
        }

        public async Task ScrapeAndSaveAsync(string? city, int? maxPage, CancellationToken ct)
        {
            var batch = new List<Vacancy>();
            const int batchSize = 50;

            await foreach (var vacancy in _vacancyScraper.ScrapeAsync(city, maxPage, ct))
            {
                if (await _vacancyRepository.ExistsAsync(vacancy.SourceId)) continue;

                batch.Add(vacancy);

                if (batch.Count >= batchSize)
                {
                    await _vacancyRepository.AddRangeAsync(batch);
                    //_logger.LogInformation($"Збережено пачку з {batch.Count} вакансій");
                    batch.Clear();
                }
            }

            if (batch.Any()) await _vacancyRepository.AddRangeAsync(batch);
        }

        //public async Task<bool> ScrapeNewAndSaveAsync(string? city, int? maxPage, CancellationToken cancellationToken = default)
        //{
        //    var urls = await _vacancyScraper.ScraperUrlAsync(city, maxPage, cancellationToken);

        //    if (!urls.Any()) return false;

        //    var idVacancy = urls
        //        .Select(url => url.Split('/'))
        //        .Where(parts => parts.Length > 2)
        //        .Select(parts => parts[4])
        //        .ToList();

        //    var existingIds = await _vacancyRepository.GetAllIdsAsync();
        //    var newVacanciesUrl = idVacancy
        //        .Where(id => !existingIds.Contains(id))
        //        .Select(id => $"https://www.work.ua/jobs/{id}/")
        //        .ToList();

        //    var newVacancies = await _vacancyScraper.ScrapeDetailsAsync(newVacanciesUrl, cancellationToken);

        //    if (!newVacancies.Any()) return false;

        //    return await _vacancyRepository.AddRangeAsync(newVacancies);
        //}

        public async Task<bool> ScrapeNotExistentDeleteAsync(string? city, int? maxPage, CancellationToken cancellationToken = default)
        {
            var currentUrls = await _vacancyScraper.ScraperUrlAsync(city, maxPage, cancellationToken);

            if (!currentUrls.Any()) return false;

            var currentIds = currentUrls
                .Select(url => url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last())
                .ToHashSet();

            var dbIds = (await _vacancyRepository.GetAllIdsAsync()).ToHashSet();
            var removedIds = dbIds.Except(currentIds).ToList();

            if (removedIds.Count == 0) return false;

            return await _vacancyRepository.RemoveRangeAsync(removedIds);
        }

        public async Task<IEnumerable<Vacancy>> GetAllAsync()
        {
            return await _vacancyRepository.GetAllAsync();
        }

        public async Task<Vacancy?> GetByIdAsync(string id)
        {
            return await _vacancyRepository.GetByIdAsync(id);
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