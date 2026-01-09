using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Application.Interfaces;
using JobVacancyCollector.Application.Services;
using JobVacancyCollector.Infrastructure.Data;
using JobVacancyCollector.Infrastructure.Parsers;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa.Html;
using JobVacancyCollector.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace JobVacancyCollector.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProjectInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddSingleton<CookieContainer>();
            services.AddScoped<IScraperFactory, ScraperFactory>();

            services.AddScoped<HtmlWorkUaParser>();
            services.AddScoped<IVacancyScraper, WorkUaParser>();

            services.AddHttpClient<IVacancyScraper, DouParser>((serviceProvider, client) =>
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

            services.AddScoped<HtmlDouParser>();
            //builder.Services.AddScoped<IVacancyScraper, DouParser>();

            services.AddScoped<IVacancyRepository, VacancyRepository>();
            services.AddScoped<IVacancyService, VacancyService>();

            services.AddSingleton<IScrapingControlService, ScrapingControlService>();

            return services;
        }
    }
}
