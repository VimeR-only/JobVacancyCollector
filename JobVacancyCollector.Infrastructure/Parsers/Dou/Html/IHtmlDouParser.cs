using AngleSharp.Dom;
using JobVacancyCollector.Domain.Models.WorkUa;

namespace JobVacancyCollector.Infrastructure.Parsers.Dou.Html
{
    public interface IHtmlDouParser
    {
        string GetTitle(IDocument doc);
        string GetSalary(IDocument doc);
        string GetCompany(IDocument doc);
        string GetLocation(IDocument doc);
        string GetContact(IDocument doc);
        string GetConditions(IDocument doc);
        string GetLanguage(IDocument doc);
        string GetDescription(IDocument doc);
    }
}
