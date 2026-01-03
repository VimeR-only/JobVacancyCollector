namespace JobVacancyCollector.Domain.Models
{
    public class Vacancy
    {
        public int Id { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Salary { get; set; } = string.Empty;
        public string SalaryNote { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string CompanyNote { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string NameContact { get; set; } = string.Empty;
        public string Conditions { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
