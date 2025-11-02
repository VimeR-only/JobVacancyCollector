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

        public async Task<bool> ScrapeAndSaveAsync(string city, int maxPage, CancellationToken cancellationToken = default)
        {
            var newVacancies = await _vacancyScraper.ScrapeAsync(city, maxPage, cancellationToken);
            
            if (newVacancies.Count() == 0) return false;

            return await _vacancyRepository.AddRangeAsync(newVacancies);
        }

        public async Task<bool> ScrapeNewAndSaveAsync(string city, int maxPage, CancellationToken cancellationToken = default)
        {
            var urls = await _vacancyScraper.ScraperUrlAsync(city, maxPage, cancellationToken);

            if (!urls.Any()) return false;

            var idVacancy = urls
                .Select(url => url.Split('/'))
                .Where(parts => parts.Length > 2)
                .Select(parts => parts[4])
                .ToList();

            var existingIds = await _vacancyRepository.GetAllIdsAsync();
            var newVacanciesUrl = idVacancy
                .Where(id => !existingIds.Contains(id))
                .Select(id => $"https://www.work.ua/jobs/{id}/")
                .ToList();

            var newVacancies = await _vacancyScraper.ScrapeDetailsAsync(newVacanciesUrl, cancellationToken);

            if (!newVacancies.Any()) return false;

            return await _vacancyRepository.AddRangeAsync(newVacancies);
        }

        public async Task<bool> ScrapeNotExistentDeleteAsync(string city, int maxPage, CancellationToken cancellationToken = default)
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
    }
}