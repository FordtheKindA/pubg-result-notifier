namespace ServicePractice.Services;
using System.Text.Json;
using System.Text;

public class DiscordWebhookService
{
    private readonly HttpClient _httpClient;

    public DiscordWebhookService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<bool> SendMessageAsync(string webhookUrl, string message)
    {
        var payload = new
        {
            content = message
        };
        string json = JsonSerializer.Serialize(payload);
        var content = new StringContent(
    json,
    Encoding.UTF8,
    "application/json"
);
var response = await _httpClient.PostAsync(webhookUrl, content);
        //return true; 
        return response.IsSuccessStatusCode;
    }
}