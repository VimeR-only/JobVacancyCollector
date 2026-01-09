using JobVacancyCollector.Infrastructure;
using JobVacancyCollector.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobVacancyCollector.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddProjectInfrastructure(builder.Configuration);

            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<ScrapingConsumer>();
                x.AddConsumer<StopScrapingConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.ReceiveEndpoint("scraping-queue", e =>
                    {
                        e.PrefetchCount = 2;
                        e.ConfigureConsumer<ScrapingConsumer>(context);
                    });

                    cfg.ReceiveEndpoint($"stop-commands-{Guid.NewGuid()}", e =>
                    {
                        e.ConfigureConsumer<StopScrapingConsumer>(context);
                    });
                });
            });

            builder.Services.AddHostedService<VacancyBackgroundJob>();

            var host = builder.Build();
            host.Run();
        }
    }
}