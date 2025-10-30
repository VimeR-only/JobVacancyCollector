using Microsoft.EntityFrameworkCore;
using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Vacancy> Vacancies { get; set; }
    }
}
