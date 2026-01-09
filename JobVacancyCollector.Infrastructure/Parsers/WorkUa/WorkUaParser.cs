using AngleSharp;
using AngleSharp.Dom;
using JobVacancyCollector.Application.Abstractions.Scrapers;
using JobVacancyCollector.Domain.Models;
using JobVacancyCollector.Infrastructure.Parsers.WorkUa.Html;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

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

        private string ToLatinCity(string city)
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

        private string GetVacancyId(string url)
        {
            return url.Split("/")[4];
        }

        public string GetIdFromUrl(string url)
        {
            return GetVacancyId(url);
        }

        public async IAsyncEnumerable<string> ScraperUrlAsync( string? cityOrOption = "Вся Україна", int? maxPage = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string normal = cityOrOption?.Trim() ?? "Вся Україна";
            string baseUrl = normal.ToLower() switch
            {
                "вся україна" => "https://www.work.ua/jobs/",
                "дистанційно" => "https://www.work.ua/jobs-remote/",
                _ => $"https://www.work.ua/jobs-{ToLatinCity(normal)}/"
            };

            int page = 1;
            bool hasVacancies = true;
            var seenUrls = new HashSet<string>();

            while (hasVacancies && (maxPage == null || page <= maxPage))
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                string url = $"{baseUrl}?page={page}";
                var document = await _context.OpenAsync(url, cancellationToken);
                var cards = document.QuerySelectorAll("div[class*='job-link']");

                if (cards.Length == 0) break;

                foreach (var card in cards)
                {
                    var link = card.QuerySelector("h2 a")?.GetAttribute("href");
                    if (!string.IsNullOrEmpty(link))
                    {
                        string fullLink = "https://www.work.ua" + link;
                        if (seenUrls.Add(fullLink))
                        {
                            yield return fullLink;
                        }
                    }
                }

                _logger.LogInformation("Page {page} URLs streamed.", page);
                page++;

                await Task.Delay(Rnd.Value.Next(500, 1500), cancellationToken);
            }
        }

        public async IAsyncEnumerable<Vacancy> ScrapeDetailsAsync(IAsyncEnumerable<string> urls, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var channel = Channel.CreateUnbounded<Vacancy>();
            int processed = 0;

            var backgroundTask = Parallel.ForEachAsync(urls, new ParallelOptions
            {
                MaxDegreeOfParallelism = 2,
                CancellationToken = ct
            }, async (url, token) =>
            {
                try
                {
                    await Task.Delay(Rnd.Value.Next(2000, 5000), token);

                    var document = await _context.OpenAsync(url, token);
                    var vacancy = HtmlVacancyPars(document);

                    if (vacancy != null)
                    {
                        vacancy.SourceId = GetVacancyId(url);
                        vacancy.SourceName = SourceName;
                        await channel.Writer.WriteAsync(vacancy, token);
                    }

                    int current = Interlocked.Increment(ref processed);
                    _logger.LogInformation("Processed vacancy #{current}: {Url}", current, url);
                }

                catch (Exception ex)
                {
                    if (ex is OperationCanceledException || ex is TaskCanceledException)
                    {
                        throw;
                    }

                    _logger.LogError(ex, "Error processing {Url}", url);
                }
            }).ContinueWith(_ => channel.Writer.Complete());

            await foreach (var v in channel.Reader.ReadAllAsync(ct))
            {
                yield return v;
            }

            await backgroundTask;
        }
        public async IAsyncEnumerable<Vacancy> ScrapeAsync(string? cityOrOption, int? maxPage, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var urls = ScraperUrlAsync(cityOrOption, maxPage, ct);

            await foreach (var vacancy in ScrapeDetailsAsync(urls, ct))
            {
                yield return vacancy;
            }
        }
    }
}
