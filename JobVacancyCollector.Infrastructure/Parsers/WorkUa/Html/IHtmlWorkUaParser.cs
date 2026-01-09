using AngleSharp.Dom;

namespace JobVacancyCollector.Infrastructure.Parsers.WorkUa.Html
{
    public interface IHtmlWorkUaParser
    {
        string GetTitle(IDocument doc);
        string GetSalary(IDocument doc, out string note);
        string GetCompany(IDocument doc, out string note);
        string GetLocation(IDocument doc, out string city);
        string GetContact(IDocument doc, out string name);
        string GetConditions(IDocument doc);
        string GetLanguage(IDocument doc);
        string GetDescription(IDocument doc);
    }
}
