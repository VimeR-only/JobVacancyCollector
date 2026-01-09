using JobVacancyCollector.Application.Interfaces;
using System.Collections.Concurrent;

namespace JobVacancyCollector.Application.Services
{
    public class ScrapingControlService : IScrapingControlService
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _stoppers = new(StringComparer.OrdinalIgnoreCase);

        public void SetStop(string siteName)
        {
            if (_stoppers.TryGetValue(siteName, out var cts))
            {
                cts.Cancel();
            }
        }

        public void SetStart(string siteName)
        {
            _stoppers.AddOrUpdate(siteName,
                _ => new CancellationTokenSource(),
                (_, old) => new CancellationTokenSource());
        }

        public CancellationToken GetLinkedToken(string siteName, CancellationToken externalToken)
        {
            var siteCts = _stoppers.GetOrAdd(siteName, _ => new CancellationTokenSource());

            return CancellationTokenSource.CreateLinkedTokenSource(siteCts.Token, externalToken).Token;
        }
    }
}