using ServicePractice.Models;

namespace ServicePractice.Services;

public class MatchResultFormatter
{
    private string GetMapDisplayName(string mapName)
{
        if (mapName == "Desert_Main")
        {
            return "Miramar";
        }
        else if (mapName == "Baltic_Main" || mapName == "Erangel_Main")
        {
            return "Erangel";
        }
        else if (mapName == "Tiger_Main")
        {
            return "Taego";
        }
        else if (mapName == "Neon_Main")
        {
            return "Rondo";
        }
        else
        {
            return mapName;
        }
}
public string FormatMany(List<MatchResult> matches)
{
    var messages = new List<string>();

    foreach (var match in matches)
    {
        messages.Add(FormatMatchResult(match));
    }

    return string.Join(
        Environment.NewLine + Environment.NewLine,
        messages
    );
}
private string FormatCreatedAt(DateTime createdAt)
{
    var thaiTime = createdAt.AddHours(7);
    return thaiTime.ToString("dd/MM/yyyy HH:mm:ss") + " GMT+7";
}
    public string FormatMatchResult(MatchResult matchResult)
    {
        var lines = new List<string>();

        lines.Add($"Match: {matchResult.MatchNumber}");
        lines.Add($"Map: {GetMapDisplayName(matchResult.MapName)}");
        lines.Add($"CreatedAt: {FormatCreatedAt(matchResult.CreatedAt)}");
        lines.Add("");

        foreach (var team in matchResult.Teams
                     .OrderBy(x => x.Placement))
        {
            lines.Add(
                $"#{team.Placement} {team.TeamTag} | " +
                $"Kills: {team.Kills} ({team.KillPoints} pts) | " +
                $"Placement: {team.PlacementPoints} pts | " +
                $"Total: {team.MatchPoints} pts"
            );
        }

        return string.Join(
            Environment.NewLine,
            lines
        );
    }
     public string FormatStandings(
    List<TeamStandingPreview> standings,
    int matchNumber){
            var lines = new List<string>();

            lines.Add($"Standings After Match {matchNumber}");
            lines.Add("");

            int rank = 1;

            foreach (var team in standings
                        .OrderByDescending(x => x.AfterPoints)
                        .ThenBy(x => x.TeamTag))
            {
                lines.Add(
                    $"{rank}. {team.TeamTag} | " +
                    $"{team.BeforePoints} +{team.MatchPoints} = {team.AfterPoints}"
                );

                rank++;
            }

            return string.Join(
                Environment.NewLine,
                lines
            );
}
}