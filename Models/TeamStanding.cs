namespace ServicePractice.Models;

public class TeamStanding
{
    public int Id { get; set; }

    public string TournamentId { get; set; } = "";
    public string TeamTag { get; set; } = "";

    public int MatchesPlayed { get; set; }

    public int TotalKills { get; set; }
    public int TotalKillPoints { get; set; }
    public int TotalPlacementPoints { get; set; }

    public int AdvancementPoints { get; set; }

    public int TotalPoints { get; set; }
}