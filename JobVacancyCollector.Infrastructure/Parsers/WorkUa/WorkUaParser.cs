using AngleSharp;
using AngleSharp.Dom;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace JobVacancyCollector.Infrastructure.Parsers.WorkUa
{
    public class WorkUaParser : IVacancyScraper
    {
        //private readonly HttpClient _httpClient;
        private readonly ILogger<WorkUaParser> _logger;
        private readonly IBrowsingContext _context;

        private static readonly ThreadLocal<Random> Rnd = new(() => new Random());

        public string SourceName { get; set; } = "Work.ua";

        public WorkUaParser()
        {
            //_httpClient = httpClient;
            //_logger = logger;

            var config = Configuration.Default.WithDefaultLoader();
            _context = BrowsingContext.New(config);
        }

        private static string GetTitle(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var titleElement = doc.QuerySelector("#h1-name");

            return titleElement?.TextContent.Trim() ?? "";
        }

        private static string GetSalary(IDocument doc, out string note)
        {
            note = "";

            if ( doc == null) return string.Empty;

            var salaryLi = doc.QuerySelectorAll("li.text-indent.no-style.mt-sm.mb-0")
                        .FirstOrDefault(li => li.QuerySelector("span.glyphicon-hryvnia-fill") != null);

            if (salaryLi == null) return string.Empty;

            var salaryElement = salaryLi.QuerySelector("span.strong-500");
            string salary = salaryElement?.TextContent.Trim() ?? "";

            var salaryNoteElement = salaryLi.QuerySelector("span.text-default-7");
            note = salaryNoteElement?.TextContent.Trim() ?? "";

            return salary;
        }

        private static string GetCompany(IDocument doc, out string note)
        {
            note = "";

            if (doc == null) return string.Empty;

            var companyLi = doc.QuerySelectorAll("li.text-indent.no-style.mt-sm.mb-0")
                                    .FirstOrDefault(li => li.QuerySelector("span.glyphicon-company") != null);
            if (companyLi == null) return string.Empty;

            var companyElement = companyLi.QuerySelector("span.strong-500");
            string company = companyElement?.TextContent.Trim() ?? "";

            var companyNoteElement = companyLi.QuerySelector("span.text-default-7");
            note = companyNoteElement?.TextContent.Trim() ?? "";

            return company;
        }

        private static string GetLocation(IDocument doc, out string city)
        {
            city = "";

            if (doc == null) return string.Empty;

            var locationLi = doc.QuerySelectorAll("li.text-indent.no-style.mt-sm.mb-0")
                        .FirstOrDefault(li => li.QuerySelector("span.glyphicon-map-marker") != null);

            if (locationLi == null) return string.Empty;

            string location = locationLi.ChildNodes
             .Where(n => n.NodeType == AngleSharp.Dom.NodeType.Text)
             .Select(n => n.TextContent.Trim())
             .Where(t => !string.IsNullOrEmpty(t))
             .FirstOrDefault() ?? "";

            city = location.Split(",")[0];

            return location;
        }

        private static string GetContact(IDocument doc, out string name)
        {
            name = "";

            if (doc == null) return string.Empty;

            var contactLi = doc.QuerySelectorAll("li.text-indent.no-style.mt-sm.mb-0")
                                    .FirstOrDefault(li => li.QuerySelector("span.glyphicon-phone") != null);

            if (contactLi == null) return string.Empty;

            var nameElement = contactLi.QuerySelector(".mr-sm");
            name = nameElement?.TextContent.Trim() ?? "";

            var phoneElement = doc.QuerySelector("span#contact-phone a.js-get-phone.js-get-phone.sendr.hidden");
            string phone = phoneElement?.GetAttribute("href")?.Replace("tel:", "").Trim() ?? "";

            return phone;
        }

        private static string GetConditions(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var conditionsLi = doc.QuerySelectorAll("li.text-indent.no-style.mt-sm.mb-0")
                                            .FirstOrDefault(li => li.QuerySelector("span.glyphicon-tick.text-default.glyphicon-large") != null);

            if (conditionsLi == null) return string.Empty;

            string conditions = string.Join(" ", conditionsLi.ChildNodes
                .Where(n => n.NodeType == AngleSharp.Dom.NodeType.Text)
                .Select(n => n.TextContent.Trim())
                .Where(t => !string.IsNullOrEmpty(t)));

            return conditions;
        }

        private static string GetLanguage(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var languageLi = doc.QuerySelectorAll("li.text-indent.no-style.mt-sm.mb-0")
                        .FirstOrDefault(li => li.QuerySelector("span.glyphicon-language") != null);

            if (languageLi == null) return string.Empty;

            string language = string.Join(" ", languageLi.ChildNodes
                .Where(n => n.NodeType == AngleSharp.Dom.NodeType.Text)
                .Select(n => n.TextContent.Trim())
                .Where(t => !string.IsNullOrEmpty(t)));

            return language;
        }

        private static string GetDescription(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var descriptionDiv = doc.QuerySelector("div#job-description");

            if (descriptionDiv == null) return string.Empty;

            string description = System.Text.RegularExpressions.Regex
                    .Replace(descriptionDiv.TextContent, @"\s+", " ")
                    .Trim();

            return description;
        }

        public async Task<IEnumerable<Vacancy>> ScrapeAsync(CancellationToken cancellationToken = default)
        {
            var vacancies = new ConcurrentBag<Vacancy>();

            try
            {
                List<string> urls = new List<string>();
                int page = 1;
                bool hasVacancies = true;

                while (hasVacancies)
                {
                    string url = $"https://www.work.ua/jobs-khmelnytskyi/?page={page}";
                    var document = await _context.OpenAsync(url);
                    var cards = document.QuerySelectorAll("div[class*='job-link']");

                    if (cards.Length == 0)
                    {
                        hasVacancies = false;

                        break;
                    }

                    foreach (var card in cards)
                    {
                        var link = card.QuerySelector("h2 a")?.GetAttribute("href");

                        if (!string.IsNullOrEmpty(link))
                        {
                            string fullLink = "https://www.work.ua" + link;

                            if (!urls.Contains(fullLink))
                                urls.Add(fullLink);

                            Console.WriteLine(fullLink);
                        }
                    }

                    Console.WriteLine($"Сторінка {page} опрацьована. Знайдено вакансій: {cards.Length}");

                    page++;

                    int delayMs = Rnd.Value.Next(500, 1500);
                    await Task.Delay(delayMs, cancellationToken);

                    //page = 125;
                }

                Console.WriteLine($"Загальна кількість вакансій: {urls.Count}");

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = cancellationToken
                };

                int id = 0;
                await Parallel.ForEachAsync(urls, options, async (url, ct) =>
                {
                    try
                    {
                        int delayMs = Rnd.Value.Next(2000, 5000);
                        await Task.Delay(delayMs, ct);

                        var config = Configuration.Default.WithDefaultLoader();
                        var localContext = BrowsingContext.New(config);
                        var document = await localContext.OpenAsync(url, ct);

                        string title = GetTitle(document);
                        string salary = GetSalary(document, out var salaryNote);
                        string company = GetCompany(document, out var companyNote);
                        string location = GetLocation(document, out var city);
                        string phone = GetContact(document, out var name);
                        string conditions = GetConditions(document);
                        string language = GetLanguage(document);
                        string description = GetDescription(document);

                        vacancies.Add(new Vacancy
                        {

                            Title = title,
                            Salary = salary,
                            SalaryNote = salaryNote,
                            Company = company,
                            CompanyNote = companyNote,
                            Location = location,
                            City = city,
                            Phone = phone,
                            NameContact = name,
                            Conditions = conditions,
                            Language = language,
                            Description = description,
                        });

                        int currentId = Interlocked.Increment(ref id);
                        Console.WriteLine($"Вакансія вставлена {currentId}");
                    }
                    catch (Exception ex)
                    {
                        // _logger.LogWarning(ex, "Помилка обробки URL {Url}", url);
                        Console.WriteLine($"Помилка обробки {url}: {ex.Message}");
                    }
                });

                Console.WriteLine($"Загальна кількість вакансій в ліст: {vacancies.Count}");
            }
            catch(Exception ex)
            {
                //_logger.LogError(ex, "Error parsing Work.ua");

                Console.WriteLine($"{ex.Message}");
            }

            return vacancies.ToList();
        }
    }
}
