
using JobVacancyCollector.Application;
using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Infrastructure.Data;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa;
using JobVacancyCollector.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JobVacancyCollector
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<IVacancyScraper, WorkUaParser>();
            builder.Services.AddScoped<IVacancyRepository, VacancyRepository>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var connectionString = builder.Configuration.GetConnectionString("WebApiDb");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    //var dbContext = services.GetRequiredService<AppDbContext>();
                    //await dbContext.Database.MigrateAsync();
                    //Console.WriteLine("Database migrations applied successfully.");

                    var scraper = services.GetRequiredService<IVacancyScraper>();

                    //var vacancies = await scraper.ScrapeAsync("Хмельницький", 1);
                    await scraper.ScrapeNewVacanciesAsync("Хмельницький", 1);

                    //var vacancyRepository = services.GetRequiredService<IVacancyRepository>();

                    //await vacancyRepository.ClearAsync();
                    //await vacancyRepository.AddRangeAsync(vacancies);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during startup (migration or scraping).");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
