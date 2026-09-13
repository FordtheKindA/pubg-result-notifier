public class MatchResult
{
    public string MatchId { get; set; } = "";
    public string TournamentId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string MapName { get; set; } = "";
    public int MatchNumber { get; set; }
    
    public List<TeamResult> Teams { get; set; } = new();
}