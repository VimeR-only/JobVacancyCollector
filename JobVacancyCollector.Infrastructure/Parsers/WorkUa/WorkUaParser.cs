using AngleSharp;
using AngleSharp.Dom;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Domain.Models.WorkUa;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa.Html;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace JobVacancyCollector.Infrastructure.Parsers.WorkUa
{
    public class WorkUaParser : IVacancyScraper
    {
        private readonly ILogger<WorkUaParser> _logger;
        private readonly IBrowsingContext _context;
        private readonly HtmlWorkUaParser _htmlWorkUaParser;

        private static readonly Random random = new Random();
        private static readonly ThreadLocal<Random> Rnd = new(() => random);

        public string SourceName { get; set; } = "Work.ua";

        public WorkUaParser(ILogger<WorkUaParser> logger, HtmlWorkUaParser htmlWorkUaParser)
        {
            _logger = logger;
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
        
        public async Task<List<string>> ScraperUrlAsync(string? cityOrOption = "Вся Україна", int? maxPage = null, CancellationToken cancellationToken = default)
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

                _logger.LogInformation($"Page {page} processed. Jobs found: {cards.Length}");

                page++;

                int delay = Rnd.Value.Next(500, 1500);
                await Task.Delay(delay, cancellationToken);
            }

            return urls;
        }

        public async Task<IEnumerable<Vacancy>> ScrapeDetailsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
        {
            var vacancies = new ConcurrentBag<Vacancy>();

            try
            {
                _logger.LogInformation($"Total number of vacancies: {urls.Count()}");

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 10,
                    CancellationToken = cancellationToken
                };

                int total = urls.Count();
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

                            _logger.LogInformation($"Job posted {currentId} з {total}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "URL processing error {Url}", url);
                    }
                });

                _logger.LogInformation($"Total number of vacancies in the list: {vacancies.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Work.ua");
            }

            return vacancies.ToList();
        }
        public async Task<IEnumerable<Vacancy>> ScrapeAsync(string? cityOrOption, int? maxPage, CancellationToken cancellationToken = default)
        {
            var urls = await ScraperUrlAsync(cityOrOption, maxPage, cancellationToken);

            return await ScrapeDetailsAsync(urls, cancellationToken);
        }
    }
}
