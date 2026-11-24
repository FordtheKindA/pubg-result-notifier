
using ServicePractice.Models;

namespace ServicePractice.Services;

public class TournamentDiscoveryService
{
    private readonly ExternalApiService _externalApiService;

    public TournamentDiscoveryService(
        ExternalApiService externalApiService)
    {
        _externalApiService = externalApiService;
    }

    public async Task<List<PubgTournamentSummary>> FindByPrefixAsync(
        string apiKey,
        string prefix)
    {
        var response = await _externalApiService.GetTournamentsAsync(apiKey);

        return response.Data
            .Where(t => t.Id.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    
public async Task<List<PubgTournamentSummary>> FindCandidatesAsync(
    string apiKey,
    string eventName,
    int year,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(eventName))
        throw new ArgumentException("Event is required");

    if (year < 2018 || year > 2100)
        throw new ArgumentOutOfRangeException(nameof(year));

    var response = await _externalApiService.GetTournamentsAsync(
        apiKey,
        cancellationToken);


    string shortYear = (year % 100).ToString("D2");

string[] keywords =
{
    $"{eventName}{shortYear}",
    $"{eventName}{year}",
    $"{eventName}-{shortYear}",
    $"{eventName}-{year}"
};

return response.Data
    .Where(t => keywords.Any(keyword =>
        t.Id.Contains(
            keyword,
            StringComparison.OrdinalIgnoreCase)))
    .ToList();
}

}
