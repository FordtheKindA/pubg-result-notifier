namespace ServicePractice.Models;

public class TeamStandingPreview
{
    public string TeamTag { get; set; } = "";

    public int BeforePoints { get; set; }
    public int MatchPoints { get; set; }
    public int AfterPoints { get; set; }

    public int TotalKills { get; set; }
    public int TotalPlacementPoints { get; set; }
}