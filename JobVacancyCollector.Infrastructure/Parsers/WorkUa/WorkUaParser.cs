using AngleSharp;
using AngleSharp.Dom;
using JobVacancyCollector.Application.Abstractions.Repositories;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Domain.Models.WorkUa;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa.Html;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Text;

namespace JobVacancyCollector.Infrastructure.Parsers.WorkUa
{
    public class WorkUaParser : IVacancyScraper
    {
        private readonly ILogger<WorkUaParser> _logger;
        private readonly IBrowsingContext _context;
        private readonly IVacancyRepository _vacancyRepository;
        private readonly HtmlWorkUaParser _htmlWorkUaParser;

        private static readonly Random random = new Random();
        private static readonly ThreadLocal<Random> Rnd = new(() => random);

        public string SourceName { get; set; } = "Work.ua";

        public WorkUaParser(ILogger<WorkUaParser> logger, IVacancyRepository vacancyRepository, HtmlWorkUaParser htmlWorkUaParser)
        {
            _logger = logger;
            _vacancyRepository = vacancyRepository;
            _htmlWorkUaParser = htmlWorkUaParser;

            var config = Configuration.Default.WithDefaultLoader();
            _context = BrowsingContext.New(config);
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

            string title = _htmlWorkUaParser.GetTitle(document);
            string salary = _htmlWorkUaParser.GetSalary(document, out var salaryNote);
            string company = _htmlWorkUaParser.GetCompany(document, out var companyNote);
            string location = _htmlWorkUaParser.GetLocation(document, out var city);
            string phone = _htmlWorkUaParser.GetContact(document, out var name);
            string conditions = _htmlWorkUaParser.GetConditions(document);
            string language = _htmlWorkUaParser.GetLanguage(document);
            string description = _htmlWorkUaParser.GetDescription(document);

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
                            vacancy.SourceId = url.Split("/")[4];

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

        public async Task<List<string>> ScrapeNotExistentVacanciesAsync(string? cityOrOption = "Вся Україна", int? maxPage = 1, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUrls = await ScraperUrlAsync(cityOrOption, maxPage, cancellationToken);
                var currentIds = currentUrls
                    .Select(url => url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last())
                    .ToHashSet();

                var dbIds = (await _vacancyRepository.GetAllIdsAsync()).ToHashSet();
                var removedIds = dbIds.Except(currentIds).ToList();

                if (removedIds.Count == 0)
                {
                    _logger.LogInformation($"Загальна кількість не актуальних вакансій: {removedIds.Count}");

                    return new List<string>();
                }

                return removedIds;
            }
            catch( Exception ex )
            {
                _logger.LogError(ex, "Error parsing Work.ua not existent vacancies");
            }

            return new List<string>();
        }
    }
}
