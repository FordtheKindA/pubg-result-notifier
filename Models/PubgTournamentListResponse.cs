
namespace ServicePractice.Models;

public class PubgTournamentListResponse
{
    public List<PubgTournamentSummary> Data { get; set; } = new();
}

public class PubgTournamentSummary
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";

    public PubgTournamentAttributes? Attributes { get; set; }
}
