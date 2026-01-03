using AngleSharp;
using AngleSharp.Dom;

namespace JobVacancyCollector.Infrastructure.Parsers.Dou.Html
{
    public class HtmlDouParser : IHtmlDouParser
    {
        private readonly IBrowsingContext _context;

        public HtmlDouParser()
        {
            var config = Configuration.Default;
            _context = BrowsingContext.New(config);
        }

        public string GetTitle(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var titleElement = doc.QuerySelector("h1.g-h2");

            return titleElement?.TextContent.Trim() ?? "";
        }

        public string GetSalary(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var salaryElement = doc.QuerySelector("span.salary");

            return salaryElement?.TextContent.Trim() ?? "";
        }

        public string GetCompany(IDocument doc)
        {
            if (doc == null) return string.Empty;


            var companyElement = doc.QuerySelector(".l-n a:first-child");

            return companyElement?.TextContent.Trim() ?? "";
        }

        public string GetLocation(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var locationElement = doc.QuerySelector("span.place.bi.bi-geo-alt-fill");

            return locationElement?.TextContent.Trim() ?? "";
        }

        public string GetContact(IDocument doc)
        {
            return "";
        }

        public string GetConditions(IDocument doc)
        {
            return "";
        }

        public string GetLanguage(IDocument doc)
        {
            return "";
        }

        public string GetDescription(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var section = doc.QuerySelector(".vacancy-section");
            if (section != null)
            {
                var lines = section.ChildNodes
                    .Select(n => n.TextContent.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t));

                string description = string.Join("\n", lines);

                return description;
            }

            return string.Empty;
        }
    }
}
