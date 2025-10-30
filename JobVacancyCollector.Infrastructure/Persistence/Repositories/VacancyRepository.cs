using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Domain.Models.WorkUa;
using JobVacancyCollector.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobVacancyCollector.Infrastructure.Persistence.Repositories
{
    public class VacancyRepository : IVacancyRepository
    {
        private readonly AppDbContext _context;

        public VacancyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Vacancy> vacancies)
        {
            var newVacancies = new List<Vacancy>();

            foreach (var vacancy in vacancies)
            {
                bool exists = await _context.Vacancies
                    .AnyAsync(v => v.SourceId == vacancy.SourceId && v.SourceName == vacancy.SourceName);

                if (!exists)
                    newVacancies.Add(vacancy);
            }

            if (newVacancies.Count > 0)
            {
                await _context.Vacancies.AddRangeAsync(newVacancies);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearAsync()
        {
            _context.Vacancies.RemoveRange(_context.Vacancies);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string? sourceId = null, string? sourceName = null)
        {
            return await _context.Vacancies.AnyAsync(v =>
                (sourceId == null || v.SourceId == sourceId) &&
                (sourceName == null || v.SourceName == sourceName));
        }

        public async Task<IEnumerable<string>> GetAllIdsAsync()
        {
            return await _context.Vacancies
                .Select(v => v.SourceId)
                .ToListAsync();
        }
    }
}
