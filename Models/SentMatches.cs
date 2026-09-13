namespace ServicePractice.Models;

public class SentMatches
{
    public int Id { get; set; }

    public string TournamentId { get; set; } = "";

    public string MatchId { get; set; } = "";

    public DateTime SentAt { get; set; }
}