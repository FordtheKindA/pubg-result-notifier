using ServicePractice.Models;

namespace ServicePractice.Services;

public class StandingsCalculator
{
    public void ApplyMatch(
        Dictionary<string, TeamStanding> standings,
        MatchResult matchResult,
        string tournamentId)
    {
        foreach (var team in matchResult.Teams)
        {
            if (!standings.TryGetValue(team.TeamTag, out var standing))
            {
                standing = new TeamStanding
                {
                    TournamentId = tournamentId,
                    TeamTag = team.TeamTag
                };
                standings.Add(team.TeamTag, standing);
            }
                standing.MatchesPlayed++;
                standing.TotalKills += team.Kills;
                standing.TotalKillPoints += team.KillPoints;
                standing.TotalPlacementPoints += team.PlacementPoints;
                standing.TotalPoints += team.MatchPoints;
                
        }
    }
    public List<TeamStandingPreview> BuildPreview(
    Dictionary<string, TeamStanding> standings,
    MatchResult matchResult)
{
    var result = new List<TeamStandingPreview>();

    foreach (var team in matchResult.Teams)
    {
        int beforePoints = 0;
        int totalKills = team.Kills;
        int totalPlacementPoints = team.PlacementPoints;

        if (standings.TryGetValue(team.TeamTag, out var standing))
        {
            beforePoints = standing.TotalPoints;
            totalKills += standing.TotalKills;
            totalPlacementPoints += standing.TotalPlacementPoints;
        }

        result.Add(new TeamStandingPreview
        {
            TeamTag = team.TeamTag,
            BeforePoints = beforePoints,
            MatchPoints = team.MatchPoints,
            AfterPoints = beforePoints + team.MatchPoints,
            TotalKills = totalKills,
            TotalPlacementPoints = totalPlacementPoints
        });
    }

    return result;
}
}