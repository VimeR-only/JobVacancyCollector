using AngleSharp.Dom;

namespace JobVacancyCollector.Infrastructure.Parsers.Dou.Html
{
    public class HtmlWorkUaParser : IHtmlWorkUaParser
    {
        public string GetTitle(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var titleElement = doc.QuerySelector("#h1-name");

            return titleElement?.TextContent.Trim() ?? "";
        }

        public string GetSalary(IDocument doc, out string note)
        {
            note = "";

            if (doc == null) return string.Empty;

            var salaryLi = doc.QuerySelectorAll("li.text-indent.no-style.mt-sm.mb-0")
                        .FirstOrDefault(li => li.QuerySelector("span.glyphicon-hryvnia-fill") != null);

            if (salaryLi == null) return string.Empty;

            var salaryElement = salaryLi.QuerySelector("span.strong-500");
            string salary = salaryElement?.TextContent.Trim() ?? "";

            var salaryNoteElement = salaryLi.QuerySelector("span.text-default-7");
            note = salaryNoteElement?.TextContent.Trim() ?? "";

            return salary;
        }

        public string GetCompany(IDocument doc, out string note)
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

        public string GetLocation(IDocument doc, out string city)
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

        public string GetContact(IDocument doc, out string name)
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

        public string GetConditions(IDocument doc)
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

        public string GetLanguage(IDocument doc)
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

        public string GetDescription(IDocument doc)
        {
            if (doc == null) return string.Empty;

            var descriptionDiv = doc.QuerySelector("div#job-description");

            if (descriptionDiv == null) return string.Empty;

            string description = System.Text.RegularExpressions.Regex
                    .Replace(descriptionDiv.TextContent, @"\s+", " ")
                    .Trim();

            return description;
        }
    }
}
