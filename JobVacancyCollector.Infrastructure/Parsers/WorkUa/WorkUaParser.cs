using AngleSharp;
using AngleSharp.Dom;
using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Domain.Models.WorkUa;
using JobVacancyCollector.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace JobVacancyCollector.Infrastructure.Parsers.WorkUa
{
    public class WorkUaParser : IVacancyScraper
    {
        private readonly ILogger<WorkUaParser> _logger;
        private readonly IBrowsingContext _context;
        private readonly IVacancyRepository _vacancyRepository;
        private static readonly Random random = new Random();
        private static readonly ThreadLocal<Random> Rnd = new(() => random);

        public string SourceName { get; set; } = "Work.ua";

        public WorkUaParser(ILogger<WorkUaParser> logger, IVacancyRepository vacancyRepository)
        {
            _logger = logger;
            _vacancyRepository = vacancyRepository;

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

        private static string ToLatinCity(string city)
        {
            var map = new Dictionary<char, string>
            {
                {'А', "A"}, {'а', "a"},
                {'Б', "B"}, {'б', "b"},
                {'В', "V"}, {'в', "v"},
                {'Г', "H"}, {'г', "h"},
                {'Ґ', "G"}, {'ґ', "g"},
                {'Д', "D"}, {'д', "d"},
                {'Е', "E"}, {'е', "e"},
                {'Є', "Ye"}, {'є', "ie"},
                {'Ж', "Zh"}, {'ж', "zh"},
                {'З', "Z"}, {'з', "z"},
                {'И', "Y"}, {'и', "y"},
                {'І', "I"}, {'і', "i"},
                {'Ї', "Yi"}, {'ї', "i"},
                {'Й', "Y"}, {'й', "i"},
                {'К', "K"}, {'к', "k"},
                {'Л', "L"}, {'л', "l"},
                {'М', "M"}, {'м', "m"},
                {'Н', "N"}, {'н', "n"},
                {'О', "O"}, {'о', "o"},
                {'П', "P"}, {'п', "p"},
                {'Р', "R"}, {'р', "r"},
                {'С', "S"}, {'с', "s"},
                {'Т', "T"}, {'т', "t"},
                {'У', "U"}, {'у', "u"},
                {'Ф', "F"}, {'ф', "f"},
                {'Х', "Kh"}, {'х', "kh"},
                {'Ц', "Ts"}, {'ц', "ts"},
                {'Ч', "Ch"}, {'ч', "ch"},
                {'Ш', "Sh"}, {'ш', "sh"},
                {'Щ', "Shch"}, {'щ', "shch"},
                {'Ю', "Yu"}, {'ю', "iu"},
                {'Я', "Ya"}, {'я', "ia"},
                {'Ь', ""}, {'ь', ""},
                {'’', ""}, {'ʼ', ""}
            };

            var result = new StringBuilder();

            foreach (var c in city)
            {
                result.Append(map.ContainsKey(c) ? map[c] : c);
            }

            return result.ToString().ToLower();
        }

        private async Task<List<string>> ScraperUrlAsync(string? cityOrOption = "Вся Україна", int? maxPage = null, CancellationToken cancellationToken = default)
        {
            List<string> urls = new List<string>();

            string baseUrl = cityOrOption?.ToLower() switch
            {
                "Вся Україна" => "https://www.work.ua/jobs/",
                "Дистанційно" => "https://www.work.ua/remote-jobs/",
                _ => $"https://www.work.ua/jobs-{ToLatinCity(cityOrOption)}/"
            };

            int page = 1;
            bool hasVacancies = true;

            while (hasVacancies && (maxPage == null || page <= maxPage))
            {
                string url = $"{baseUrl}?page={page}";

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

                        _logger.LogInformation(fullLink);
                    }
                }

                _logger.LogInformation($"Сторінка {page} опрацьована. Знайдено вакансій: {cards.Length}");

                page++;

                int delay = Rnd.Value.Next(500, 1500);
                await Task.Delay(delay, cancellationToken);
            }

            return urls;
        }

        private Vacancy? HtmlVacancyPars(IDocument document)
        {
            if (document == null) return null;

            string title = GetTitle(document);
            string salary = GetSalary(document, out var salaryNote);
            string company = GetCompany(document, out var companyNote);
            string location = GetLocation(document, out var city);
            string phone = GetContact(document, out var name);
            string conditions = GetConditions(document);
            string language = GetLanguage(document);
            string description = GetDescription(document);

            var vacancy = new Vacancy {
                SourceName = SourceName,
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
            };

            return vacancy;
        }

        public async Task<IEnumerable<Vacancy>> ScrapeAsync(string? cityOrOption = "Вся Україна", int? maxPage = null, CancellationToken cancellationToken = default, IProgress<int>? progress = null)
        {
            var vacancies = new ConcurrentBag<Vacancy>();

            try
            {
                var urls = await ScraperUrlAsync(cityOrOption, maxPage, cancellationToken);

                _logger.LogInformation($"Загальна кількість вакансій: {urls.Count}");

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = cancellationToken
                };

                int total = urls.Count();
                int processed = 0;
                int id = 0;

                await Parallel.ForEachAsync(urls, options, async (url, ct) =>
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
                            vacancies.Add(vacancy);

                            _logger.LogInformation($"Вакансія вставлена {currentId} з {total}");
                        }

                        int done = Interlocked.Increment(ref processed);
                        int percent = (int)((double)done / total * 100);

                        progress?.Report(percent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Помилка обробки URL {Url}", url);
                    }
                });

                _logger.LogInformation($"Загальна кількість вакансій в ліст: {vacancies.Count}");

                progress?.Report(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Work.ua");
            }

            return vacancies.ToList();
        }

        public async Task<IEnumerable<Vacancy>> ScrapeNewVacanciesAsync(string? cityOrOption = "Вся Україна", int? maxPage = 1, CancellationToken cancellationToken = default)
        {
            var vacancies = new ConcurrentBag<Vacancy>();

            try
            {
                var urls = await ScraperUrlAsync(cityOrOption, maxPage, cancellationToken);

                var idVacancy = urls
                    .Select(url => url.Split('/'))
                    .Where(parts => parts.Length > 2)
                    .Select(parts => parts[4])
                    .ToList();

                var existingIds = await _vacancyRepository.GetAllIdsAsync();
                var newVacancies = idVacancy.Where(id => !existingIds.Contains(id)).ToList();

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = cancellationToken
                };

                await Parallel.ForEachAsync(newVacancies, options, async (id, ct) =>
                {
                    try
                    {
                        int delayMs = Rnd.Value.Next(2000, 5000);
                        await Task.Delay(delayMs, ct);

                        var fullUrl = $"https://www.work.ua/jobs/{id}/";
                        var document = await _context.OpenAsync(fullUrl, ct);
                        var vacancy = HtmlVacancyPars(document);

                        if (vacancy != null)
                        {
                            vacancies.Add(vacancy);

                            Console.WriteLine($"Найдена нова вакансія {id} ");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Помилка обробки URL {Url}", id);
                    }
                });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error parsing Work.ua New Vacancies");
            }

            return vacancies.ToList();
        }
    }
}
