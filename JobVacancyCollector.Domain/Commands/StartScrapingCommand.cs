namespace JobVacancyCollector.Domain.Commands
{
    public record StartScrapingCommand
    {
        public string Site { get; init; }
        public string? City { get; init; }
        public int? MaxPages { get; init; }
    }
}
