namespace ServicePractice.Models;

public class PubgTournamentResponse
{
    public PubgTournamentData Data { get; set; } = new();
    public List<PubgTournamentIncluded> Included { get; set; } = new();
}

public class PubgTournamentData
{
    public string Id { get; set; } = "";
}

public class PubgTournamentIncluded
{
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    public PubgTournamentAttributes? Attributes { get; set; }
}

public class PubgTournamentAttributes
{
    public DateTime CreatedAt { get; set; }
}