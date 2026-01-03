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

        public async Task<bool> RemoveIdAsync(string sourceId)
        {
            var vacancy = await _context.Vacancies.FirstOrDefaultAsync(v => v.SourceId == sourceId);

            if (vacancy != null)
            {
                _context.Vacancies.Remove(vacancy);
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }

        public async Task<bool> RemoveRangeAsync(IEnumerable<string> sourceIds)
        {
            var vacancies = _context.Vacancies.Where(v => sourceIds.Contains(v.SourceId));

            _context.Vacancies.RemoveRange(vacancies);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Vacancy>> GetAllAsync()
        {
            return await _context.Vacancies.ToListAsync();
        }

        public async Task<Vacancy?> GetByIdAsync(string id)
        {
            return await _context.Vacancies.FirstOrDefaultAsync(v => v.SourceId == id);
        }
    }
}
