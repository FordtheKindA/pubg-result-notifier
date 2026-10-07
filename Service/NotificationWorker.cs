using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ServicePractice.Data;
using ServicePractice.Models;
using System.Text.RegularExpressions;



namespace ServicePractice.Services;

public class NotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MatchResultFormatter _formatter;
    
    private readonly IConfiguration _configuration;
    private readonly ScoringCalculator _scoringCalculator;
    private readonly StandingsCalculator _standingsCalculator;
    
    

    public NotificationWorker(
    IServiceScopeFactory scopeFactory,
    MatchResultFormatter formatter,
    IConfiguration configuration,
    ScoringCalculator scoringCalculator,
    StandingsCalculator standingsCalculator)
{
    _scopeFactory = scopeFactory;
    _formatter = formatter;
    _configuration = configuration;
    _scoringCalculator = scoringCalculator;
    _standingsCalculator = standingsCalculator;
}


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var externalApiService =
                scope.ServiceProvider
                    .GetRequiredService<ExternalApiService>();

            //string tournamentId = "sea-ctga";
            string tournamentId = "sea-ptsgf";
            string apiKey =_configuration["Pubg:ApiKey"] ?? throw new InvalidOperationException("PUBG API key is not configured");


            var tournament =
            await externalApiService.GetTournamentAsync(tournamentId,apiKey);

            if (tournament == null)
            {
                Console.WriteLine("Tournament not found");

                await Task.Delay(
                    TimeSpan.FromSeconds(60),
                    stoppingToken
                );

                continue;
            }

            var matches = tournament.Included
                .Where(x => x.Type == "match")
                .OrderBy(x => x.Attributes?.CreatedAt)
                .ToList();
            var standings = await dbContext.TeamStandings
                .Where(x => x.TournamentId == tournamentId)
                .ToDictionaryAsync(
                    x => x.TeamTag,
                    stoppingToken
                );
            if (standings.Count == 0)
            {
                var sentMatchIds = await dbContext.SentMatches
                    .Where(x => x.TournamentId == tournamentId)
                    .Select(x => x.MatchId)
                    .ToHashSetAsync(stoppingToken);

                if (sentMatchIds.Count > 0)
                {
                    Console.WriteLine(
                        $"Backfilling standings from {sentMatchIds.Count} sent matches..."
                    );

                    for (int i = 0; i < matches.Count; i++)
                    {
                        var currentMatch = matches[i];

                        if (!sentMatchIds.Contains(currentMatch.Id))
                        {
                            continue;
                        }

                        var matchResult =
                            await BuildMatchResultAsync(
                                currentMatch.Id,
                                i + 1,
                                tournamentId,
                                externalApiService
                            );

                        if (matchResult == null)
                        {
                            continue;
                        }

                        _standingsCalculator.ApplyMatch(
                            standings,
                            matchResult,
                            tournamentId
                        );
                    }

                    foreach (var standing in standings.Values)
                    {
                        if (standing.Id == 0)
                        {
                            dbContext.TeamStandings.Add(standing);
                        }
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);

                    Console.WriteLine(
                        $"Backfill complete: {standings.Count} teams"
                    );
                }
            }

            Console.WriteLine($"Loaded {standings.Count} team standings");
            foreach (var standing in standings.Values
             .OrderByDescending(x => x.TotalPoints))
                {
                    Console.WriteLine(
                        $"{standing.TeamTag} | " +
                        $"MP: {standing.MatchesPlayed} | " +
                        $"Kills: {standing.TotalKills} | " +
                        $"PlacementPts: {standing.TotalPlacementPoints} | " +
                        $"Total: {standing.TotalPoints}"
                    );
                }
            
            List<MatchResult> newMatchResults = new();

            for(int i=0;i<matches.Count;i++)
            {
                var currentMatch = matches[i];
                bool alreadySent =
                            await dbContext.SentMatches
                                .AnyAsync(
                                    x => x.MatchId == currentMatch.Id,
                                    stoppingToken
                                );

                        if (alreadySent)
                        {
                            continue;
                        }

                var matchResult =
                    await BuildMatchResultAsync(
                        currentMatch.Id,
                        i + 1,
                        tournamentId,
                        externalApiService
                    );

                if (matchResult == null)
                {
                    continue;
                }

                Console.WriteLine(
                    $"Match: {matchResult.MatchId} | " +
                    $"Map: {matchResult.MapName} | " +
                    $"Teams: {matchResult.Teams.Count}"
                );

                    newMatchResults.Add(matchResult);
            }

            if (newMatchResults.Count > 0)
            {
                var discordWebhookService =
                    scope.ServiceProvider
                        .GetRequiredService<DiscordWebhookService>();

                string webhookUrl =
                    _configuration["Discord:WebhookUrl"]
                    ?? throw new InvalidOperationException(
                        "Discord webhook is not configured"
                    );

                foreach (var matchResult in newMatchResults)
                {
                    var preview =_standingsCalculator.BuildPreview(standings,matchResult);
                    string matchMessage =_formatter.FormatMatchResult(matchResult);

                    string standingsMessage =
                        _formatter.FormatStandings(
                            preview,
                            matchResult.MatchNumber
                        );

                    string message =
                        matchMessage +
                        Environment.NewLine +
                        Environment.NewLine +
                        standingsMessage;
                        

                    Console.WriteLine(
                        $"Sending Match {matchResult.MatchNumber} | " +
                        $"Length: {message.Length}"
                    );

                    bool sent =
                        await discordWebhookService.SendMessageAsync(
                            webhookUrl,
                            message
                        );

                    if (sent)
                    {
                        Console.WriteLine(
                            $"Match {matchResult.MatchNumber} send success"
                        );
                        _standingsCalculator.ApplyMatch(
                            standings,
                            matchResult,
                            tournamentId
                        );

                        foreach (var standing in standings.Values)
                        {
                            if (standing.Id == 0)
                            {
                                dbContext.TeamStandings.Add(standing);
                            }
                        }

                        dbContext.SentMatches.Add(
                            new SentMatches
                            {
                                TournamentId = tournamentId,
                                MatchId = matchResult.MatchId,
                                SentAt = DateTime.UtcNow
                            }
                        );

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Match {matchResult.MatchNumber} send failed");
                            break;
                    }
                }
            }
            
            await Task.Delay(
                TimeSpan.FromSeconds(120),
                stoppingToken
            );
        }
    }
    private async Task<MatchResult?> BuildMatchResultAsync(
    string matchId,
    int matchNumber,
    string tournamentId,
    ExternalApiService externalApiService){
            var match =
                await externalApiService.GetPubgMatchAsync(matchId);

            if (match == null)
            {
                return null;
            }

            var rosters = match.Included
                .Where(x => x.Type == "roster")
                .ToList();

            var participants = match.Included
                .Where(x => x.Type == "participant")
                .ToList();

            List<TeamResult> teamResults = new();

            foreach (var roster in rosters)
            {
                var participantIds =
                    roster.Relationships?
                        .Participants?
                        .Data;

                if (participantIds == null)
                {
                    continue;
                }

                string teamTag = "";
                int totalKills = 0;

                foreach (var p in participantIds)
                {
                    var player = participants
                        .FirstOrDefault(x => x.Id == p.Id);

                    if (player == null)
                    {
                        continue;
                    }

                    totalKills +=
                        player.Attributes?
                            .Stats?
                            .Kills ?? 0;

                    if (string.IsNullOrEmpty(teamTag))
                    {
                        string name =
                            player.Attributes?
                                .Stats?
                                .Name ?? "";

                        if (name.Contains('_'))
                        {
                            teamTag = name.Split('_')[0];
                        }
                    }
                }

                int placement =
                    roster.Attributes?
                        .Stats?
                        .Rank ?? 0;

                int killPoints =
                    _scoringCalculator.GetKillPoints(totalKills);

                int placementPoints =
                    _scoringCalculator.GetPlacementPoints(placement);

                var teamResult = new TeamResult
                {
                    TeamTag = teamTag,
                    Placement = placement,
                    Kills = totalKills,
                    KillPoints = killPoints,
                    PlacementPoints = placementPoints,
                    MatchPoints = killPoints + placementPoints
                };

                teamResults.Add(teamResult);
            }

            return new MatchResult
            {
                MatchId = match.Data.Id,
                TournamentId = tournamentId,
                CreatedAt = match.Data.Attributes.CreatedAt,
                MapName = match.Data.Attributes.MapName,
                MatchNumber = matchNumber,
                Teams = teamResults
            };
}
}

        
