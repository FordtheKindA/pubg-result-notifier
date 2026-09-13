using ServicePractice.Models;
using System.Text.Json;

namespace ServicePractice.Services;

public class ExternalApiService
{
private readonly HttpClient _httpClient;

public ExternalApiService(HttpClient httpClient)
{
    _httpClient = httpClient;
}
public async Task<TodoResponse?> GetDataAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        string content = await response.Content.ReadAsStringAsync();
        var todo = JsonSerializer.Deserialize<TodoResponse>(
        content,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }
);
        return todo;
    }

public async Task<PubgMatchResponse?> GetPubgMatchAsync(string matchId)
{
    string url =
        $"https://api.pubg.com/shards/tournament/matches/{matchId}";

    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Accept.ParseAdd("application/vnd.api+json");

    var response = await _httpClient.SendAsync(request);

    if (!response.IsSuccessStatusCode)
    {
        return null;
    }

    string json = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<PubgMatchResponse>(
        json,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }
    );
}
public async Task<PubgTournamentResponse?> GetTournamentAsync(
    string tournamentId,
    string apiKey)
{
    string url =
        $"https://api.pubg.com/tournaments/{tournamentId}";

    var request =
        new HttpRequestMessage(HttpMethod.Get, url);

    request.Headers.Accept.ParseAdd(
        "application/vnd.api+json"
    );

    request.Headers.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            apiKey
        );

    var response =
        await _httpClient.SendAsync(request);

    if (!response.IsSuccessStatusCode)
    {
        return null;
    }

    string json =
        await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<PubgTournamentResponse>(
        json,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }
    );
}

}
