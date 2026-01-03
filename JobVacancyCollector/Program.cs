using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Application.Services;
using JobVacancyCollector.Infrastructure.Data;
using JobVacancyCollector.Infrastructure.Parsers;
using JobVacancyCollector.Infrastructure.Parsers.Dou.Html;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa.Html;
using JobVacancyCollector.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json.Serialization;

namespace JobVacancyCollector
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Add services to the container.
            builder.Services.AddSingleton<CookieContainer>();
            builder.Services.AddScoped<IScraperFactory, ScraperFactory>();

            builder.Services.AddScoped<HtmlWorkUaParser>();
            builder.Services.AddScoped<IVacancyScraper, WorkUaParser>();

            builder.Services.AddHttpClient<IVacancyScraper, DouParser>((serviceProvider, client) =>
            {
                client.BaseAddress = new Uri("https://jobs.dou.ua/");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                client.DefaultRequestHeaders.Add("Referer", "https://jobs.dou.ua/vacancies/");
            })
            .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
            {
                var cookieContainer = serviceProvider.GetRequiredService<CookieContainer>();

                return new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true,
                    AllowAutoRedirect = true
                };
            });

            builder.Services.AddScoped<HtmlDouParser>();
            builder.Services.AddScoped<IVacancyScraper, DouParser>();

            builder.Services.AddScoped<IVacancyRepository, VacancyRepository>();
            builder.Services.AddScoped<IVacancyService, VacancyService>();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
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

                    //var scraper = services.GetRequiredService<IVacancyScraper>();

                    //var vacancies = await scraper.ScrapeAsync("Хмельницький", 1);
                    //await scraper.ScrapeNewVacanciesAsync("Хмельницький", 1);

                    //var vacancyRepository = services.GetRequiredService<IVacancyRepository>();

                    //await vacancyRepository.ClearAsync();
                    //await vacancyRepository.AddRangeAsync(vacancies);

                    //var vacancies2 = await scraper.ScrapeNotExistentVacanciesAsync("Хмельницький", 1);

                    //Console.WriteLine(vacancies2.Count);

                    //var scrapers = services.GetServices<IVacancyScraper>();
                    //var douScraper = scrapers.FirstOrDefault(s => s is DouParser);

                    //if (douScraper != null)
                    //{
                    //    using var cts = new CancellationTokenSource();
                    //    var ct = cts.Token;

                    //    var vacancyStream = douScraper.ScrapeAsync("Дніпро", 1, ct);

                    //    await foreach (var vacancy in vacancyStream.WithCancellation(ct))
                    //    {
                    //        Console.WriteLine($"Отримано: {vacancy.Title} від {vacancy.Company}");
                    //        // await _repository.AddAsync(vacancy);
                    //    }
                    //}
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
