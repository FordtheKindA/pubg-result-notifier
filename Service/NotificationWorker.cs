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
    

    public NotificationWorker(
    IServiceScopeFactory scopeFactory,
    MatchResultFormatter formatter,
    IConfiguration configuration)
    {
    _scopeFactory = scopeFactory;
    _formatter = formatter;
    _configuration = configuration;
    } 


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext =
    scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

            var externalApiService =
                scope.ServiceProvider
                    .GetRequiredService<ExternalApiService>();

            //string tournamentId = "sea-ctga";
            string tournamentId = "sea-ctf3";
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

                var match =
                    await externalApiService.GetPubgMatchAsync(
                        currentMatch.Id
                    );

                if (match == null)
                {
                    continue;
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

                    var teamResult = new TeamResult
                    {
                        TeamTag = teamTag,
                        Placement = placement,
                        Kills = totalKills
                    };

                    teamResults.Add(teamResult);
                }

                var matchResult = new MatchResult
                {
                    MatchId = match.Data.Id,
                    TournamentId = tournamentId,
                    CreatedAt = match.Data.Attributes.CreatedAt,
                    MapName = match.Data.Attributes.MapName,
                    MatchNumber =i+1,
                    Teams = teamResults
                };
                
                Console.WriteLine(
                    $"Match: {matchResult.MatchId} | " +
                    $"Map: {matchResult.MapName} | " +
                    $"Teams: {matchResult.Teams.Count}"
                );
                newMatchResults.Add(matchResult);
                }

        if (newMatchResults.Count > 0)
            {
                        
                string message = _formatter.FormatMany(newMatchResults);

                    Console.WriteLine(message);
                
                
            

        var discordWebhookService =
            scope.ServiceProvider
                .GetRequiredService<DiscordWebhookService>();

        string webhookUrl =_configuration["Discord:WebhookUrl"]?? throw new InvalidOperationException("Discord webhook is not configured"); 

        bool sent = await discordWebhookService.SendMessageAsync(
            webhookUrl,
            message
        );

        if (sent)
        {
            Console.WriteLine("Discord send success");

            foreach (var matchResult in newMatchResults)
            {
                dbContext.SentMatches.Add(
                    new SentMatches
                    {
                        TournamentId = tournamentId,
                        MatchId = matchResult.MatchId,
                        SentAt = DateTime.UtcNow
                    }
                );
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            }
            else
            {
            Console.WriteLine("Discord send failed");
            }
            }
            
            await Task.Delay(
                TimeSpan.FromSeconds(120),
                stoppingToken
            );
            }
 }
}

        
