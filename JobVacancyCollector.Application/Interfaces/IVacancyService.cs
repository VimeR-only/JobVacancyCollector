using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using JobVacancyCollector.Domain.Models;
using System.IO;

namespace JobVacancyCollector.Application.Interfaces
{
    public interface IVacancyService
    {
        Task ScrapeAndSaveAsync(string site, string? city, int? maxPage, CancellationToken ct);
        //Task<bool> ScrapeNewAndSaveAsync(string? city, int? maxPage, CancellationToken cancellationToken = default);
        Task<bool> ScrapeNotExistentDeleteAsync(string site, string? city, int? maxPage, CancellationToken cancellationToken = default);
        Task<IEnumerable<Vacancy>> GetAllAsync();
        Task<Vacancy?> GetByIdAsync(string id);
        Task ClearDb();
        Task<MemoryStream> ExportExcel(); //Temporarily added for github users.I don't need it, but it will be made better later.
    }
}
