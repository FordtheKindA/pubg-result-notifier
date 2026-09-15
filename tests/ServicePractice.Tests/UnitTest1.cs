using ServicePractice.Models;
using ServicePractice.Services;

namespace ServicePractice.Tests;

public class MatchResultFormatterTests
{
    [Fact]
    public void FormatMatchResult_DesertMain_ShouldDisplayMiramar()
    {
        var formatter = new MatchResultFormatter();
        var match = new MatchResult
        { 
            MatchNumber = 1,
            MapName = "Desert_Main",
            CreatedAt = new DateTime(2026, 9, 15, 12, 0, 0),
            Teams = new List<TeamResult>()
        }; 
        var result = formatter.FormatMatchResult(match);
        Assert.Contains("Map: Miramar", result);

    }
    [Fact]
    public void FormatMatchResult_TigerMain_ShouldDisplayTaego()
    {
        var formatter = new MatchResultFormatter();
        var match = new MatchResult
        { 
            MatchNumber = 1,
            MapName = "Tiger_Main",
            CreatedAt = new DateTime(2026, 9, 15, 12, 0, 0),
            Teams = new List<TeamResult>()
        }; 
        var result = formatter.FormatMatchResult(match);
        Assert.Contains("Map: Taego", result);

    }
    [Fact]
    public void FormatMatchResult_OneTeam_ShouldDisplayTeamResult()
    {
        // Arrange
        var formatter = new MatchResultFormatter();

        var match = new MatchResult
        {
            MatchNumber = 1,
            MapName = "Desert_Main",
            CreatedAt = new DateTime(2026, 9, 15, 12, 0, 0),
            Teams = new List<TeamResult>
            {
                new TeamResult
                {
                    TeamTag = "MiTH",
                    Placement = 1,
                    Kills = 10
                }
            }
        };

       
        var result = formatter.FormatMatchResult(match);

        
        Assert.Contains("#1 MiTH | Kills: 10", result);
    }
    
}