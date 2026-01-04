using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Domain.Models;
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
            await _context.Vacancies.AddRangeAsync(vacancies);
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();
        }

        public async Task<bool> ClearAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Vacancies\" RESTART IDENTITY;");

            return true;
        }

        public async Task<bool> ExistsAsync(string sourceName, string sourceId)
        {
            return await _context.Vacancies.AnyAsync(v =>
                v.SourceName == sourceName &&
                v.SourceId == sourceId);
        }

        public async Task<IEnumerable<string>> GetAllIdsAsync(string? sourceName = null)
        {
            if (sourceName == null)
            {
                return await _context.Vacancies
                .Select(v => v.SourceId)
                .ToListAsync();
            }

            return await _context.Vacancies.Where(v => v.SourceName == sourceName).Select(v => v.SourceId)
                .ToListAsync();
        }

        public async Task<bool> RemoveIdAsync(string sourceName, string sourceId)
        {
            var vacancy = await _context.Vacancies.FirstOrDefaultAsync(v => v.SourceName == sourceName && v.SourceId == sourceId);

            if (vacancy == null) return false;

            _context.Vacancies.Remove(vacancy);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveRangeAsync(string sourceName, IEnumerable<string> sourceIds)
        {
            await _context.Vacancies
                    .Where(v => v.SourceName == sourceName && sourceIds.Contains(v.SourceId))
                    .ExecuteDeleteAsync();

            return true;
        }

        public async Task<IEnumerable<Vacancy>> GetAllAsync(string? sourceName = null)
        {
            if (sourceName == null)
            {
                return await _context.Vacancies.ToListAsync();
            }

            return await _context.Vacancies.Where(v => v.SourceName == sourceName).ToListAsync();
        }

        public async Task<Vacancy?> GetByIdAsync(string sourceName, string id)
        {
            return await _context.Vacancies.FirstOrDefaultAsync(v => v.SourceName == sourceName && v.SourceId == id);
        }
    }
}
