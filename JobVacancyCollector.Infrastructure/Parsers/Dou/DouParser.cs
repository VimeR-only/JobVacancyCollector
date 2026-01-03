using AngleSharp;
using AngleSharp.Dom;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Infrastructure.Parsers.Dou.Html;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using JobVacancyCollector.Domain.Models;

namespace JobVacancyCollector.Infrastructure.Parsers.WorkUa
{
    public class DouParser : IVacancyScraper
    {
        private readonly HttpClient _client;

        private readonly ILogger<DouParser> _logger;
        private readonly IBrowsingContext _context;
        private readonly HtmlDouParser _htmlDouParser;

        private static readonly Random random = new Random();
        private static readonly ThreadLocal<Random> Rnd = new(() => random);

        public string SourceName { get; set; } = "jobs.dou.ua";

        public DouParser(HttpClient client, ILogger<DouParser> logger, HtmlDouParser htmlDouParser)
        {
            _client = client;
            _logger = logger;
            _htmlDouParser = htmlDouParser;

            var config = Configuration.Default.WithDefaultLoader();
            _context = BrowsingContext.New(config);
        }

        private string? ExtractToken(string html)
        {
            var match = Regex.Match(html, @"window\.CSRF_TOKEN\s*=\s*""(.*?)""");
            if (match.Success) return match.Groups[1].Value;

            match = Regex.Match(html, @"name=""csrfmiddlewaretoken""\s+value=""(.*?)""");
            if (match.Success) return match.Groups[1].Value;

            return null;
        }

        private string GetVacancyId(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            string trimdUrl = url.EndsWith("/")
                ? url.Substring(0, url.Length - 1)
                : url;

            int lastIndex = trimdUrl.LastIndexOf('/');

            if (lastIndex != -1)
            {
                return trimdUrl.Substring(lastIndex + 1);
            }

            return string.Empty;
        }

        private Vacancy? HtmlVacancyPars(IDocument document)
        {
            if (document == null) return null;

            string title = _htmlDouParser.GetTitle(document);
            string salary = _htmlDouParser.GetSalary(document);
            string company = _htmlDouParser.GetCompany(document);
            string location = _htmlDouParser.GetLocation(document);
            string description = _htmlDouParser.GetDescription(document);

            //Console.WriteLine(company + " " + title + " " + salary);
            //Console.WriteLine(location);
            //Console.WriteLine(description);

            var vacancy = new Vacancy
            {
                SourceName = SourceName,
                Title = title,
                Salary = salary,
                Company = company,
                Location = location,
                Description = description,
            };

            return vacancy;
        }

        private async Task<(List<string> urls, bool isLast)> FetchVacancyUrls(string city, string token, int count, CancellationToken ct)
        {
            var url = "https://jobs.dou.ua/vacancies/xhr-load/";
            if (!string.IsNullOrEmpty(city))
            {
                url += $"?city={Uri.EscapeDataString(city)}";
            }

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "csrfmiddlewaretoken", token },
                { "count", count.ToString() }
            });

            var response = await _client.PostAsync(url, content, ct);
            if (!response.IsSuccessStatusCode) return (new List<string>(), true);

            var json = await response.Content.ReadAsStringAsync(ct);
            using JsonDocument doc = JsonDocument.Parse(json);

            var html = doc.RootElement.GetProperty("html").GetString();
            var isLast = doc.RootElement.GetProperty("last").GetBoolean();

            var document = await _context.OpenAsync(req => req.Content(html), ct);
            var links = document.QuerySelectorAll("a.vt")
                                .Select(m => m.GetAttribute("href"))
                                .Where(l => !string.IsNullOrEmpty(l))
                                .Select(l =>
                                {
                                    int index = l!.IndexOf('?');
                                    return index > 0 ? l.Substring(0, index) : l;
                                })
                                .Distinct()
                                .ToList();

            return (links, isLast);
        }

        public async Task<List<string>> ScraperUrlAsync(string? cityOrOption = "", int? maxPage = null, CancellationToken cancellationToken = default)
        {
            List<string> allUrls = new List<string>();

            try
            {
                var url = "https://jobs.dou.ua/vacancies/";

                if (!string.IsNullOrEmpty(cityOrOption))
                {
                    url += $"?city={cityOrOption}";
                }

                var mainPage = await _client.GetStringAsync(url, cancellationToken);
                var token = ExtractToken(mainPage);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token not found");
                    return allUrls;
                }

                //Console.WriteLine("Token: " + token);

                int currentCount = 0;
                bool isLast = false;
                int pagesCollected = 0;

                while (!isLast && (!maxPage.HasValue || pagesCollected < maxPage.Value))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    _logger.LogInformation("Scraping DOU vacancies offset: {count}", currentCount);

                    var (urls, last) = await FetchVacancyUrls(cityOrOption, token, currentCount, cancellationToken);
                    allUrls.AddRange(urls);

                    isLast = last;
                    currentCount += 20;
                    pagesCollected++;

                    if (!isLast) await Task.Delay(1500, cancellationToken);
                }

                Console.WriteLine("Cout: " + allUrls.Count);
                foreach (var item in allUrls)
                {
                    Console.WriteLine(item);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Error scrape url");

            }

            return allUrls;
        }
        public async IAsyncEnumerable<Vacancy> ScrapeDetailsAsync(IEnumerable<string> urls, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<Vacancy>();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 10,
                CancellationToken = cancellationToken
            };

            int total = urls.Count();
            int id = 0;

            var backgroundTask = Parallel.ForEachAsync(urls, options, async (url, ct) =>
            {
                try
                {
                    int delayMs = Rnd.Value.Next(2000, 5000);
                    await Task.Delay(delayMs, ct);

                    int currentId = Interlocked.Increment(ref id);
                    var document = await _context.OpenAsync(url, ct);
                    var vacancy = HtmlVacancyPars(document);

                    if (vacancy != null)
                    {
                        vacancy.SourceId = GetVacancyId(url);
                        vacancy.SourceName = SourceName;

                        await channel.Writer.WriteAsync(vacancy, ct);

                        _logger.LogInformation($"{currentId} out of {total} jobs processed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "URL processing error {Url}", url);
                }
            }).ContinueWith(_ => channel.Writer.Complete());

            await foreach (var vacancy in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return vacancy;
            }


            await backgroundTask;
        }
        public async IAsyncEnumerable<Vacancy> ScrapeAsync(string? cityOrOption, int? maxPage, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var urls = await ScraperUrlAsync(cityOrOption, maxPage, cancellationToken);

            await foreach (var vacancy in ScrapeDetailsAsync(urls, cancellationToken))
            {
                yield return vacancy;
            }
        }
    }
}
