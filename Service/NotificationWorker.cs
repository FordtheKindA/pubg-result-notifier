using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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
    private readonly ILogger<NotificationWorker> _logger;
    
    

    public NotificationWorker(
    IServiceScopeFactory scopeFactory,
    MatchResultFormatter formatter,
    IConfiguration configuration,
    ScoringCalculator scoringCalculator,
    StandingsCalculator standingsCalculator,
    ILogger<NotificationWorker> logger)
    {
    _scopeFactory = scopeFactory;
    _formatter = formatter;
    _configuration = configuration;
    _scoringCalculator = scoringCalculator;
    _standingsCalculator = standingsCalculator;
    _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var candidateTournamentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        DateTimeOffset nextDiscoveryAt = DateTimeOffset.MinValue;
        DateTimeOffset nextMatchAt = DateTimeOffset.MinValue;
        string cutoffText = _configuration["Pubg:ProcessingNotBeforeUtc"]?? throw new InvalidOperationException(
        "ProcessingNotBeforeUtc is missing");

        if (!DateTimeOffset.TryParse(
            cutoffText,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
            out var processingNotBeforeUtc))
        {
            throw new InvalidOperationException(
                "ProcessingNotBeforeUtc has an invalid date format");
        }

        _logger.LogInformation(
            "Processing cutoff (UTC): {Cutoff}",
            processingNotBeforeUtc
        );
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var externalApiService =
                scope.ServiceProvider
                    .GetRequiredService<ExternalApiService>();
            var discoveryService = scope.ServiceProvider
                .GetRequiredService<TournamentDiscoveryService>();

            
            
            string apiKey =_configuration["Pubg:ApiKey"] ?? throw new InvalidOperationException("PUBG API key is not configured");
            string eventName = _configuration["Pubg:TournamentSearch:Event"]
                ?? throw new InvalidOperationException("Event is missing");

            int year = _configuration.GetValue<int>(
                "Pubg:TournamentSearch:Year");

            if (DateTimeOffset.UtcNow >= nextDiscoveryAt)
            {
                nextDiscoveryAt = DateTimeOffset.UtcNow.AddMinutes(5);

                try
                {
                var candidates = await discoveryService.FindCandidatesAsync(
                    apiKey,
                    eventName,
                    year,
                    stoppingToken
                 );

                    _logger.LogInformation(
                    "Tournament Discovery found {Count} candidates",
                        candidates.Count
                    );
                    candidateTournamentIds.Clear();
                    foreach (var candidate in candidates)
                    {
                        candidateTournamentIds.Add(candidate.Id);
                    }

                    _logger.LogInformation(
                     "Cached tournament candidates: {Count}",
                        candidateTournamentIds.Count
                    );
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Tournament Discovery API request failed"
                    );
                }
            }
            
            
            if (_configuration.GetValue<bool>("Pubg:DiscoveryOnly"))
            {
                if (DateTimeOffset.UtcNow >= nextMatchAt)
                {
                    nextMatchAt = DateTimeOffset.UtcNow.AddMinutes(2);

                    _logger.LogInformation(
                        "[DRY RUN] Match polling due — processing skipped"
                    );
                    foreach (var tournamentIdCandidate in candidateTournamentIds)
                    {
                        _logger.LogInformation(
                            "[DRY RUN] Would check matches for {TournamentId}",
                            tournamentIdCandidate
                        );
                        try
                        {
                            
                            var candidateTournament = await externalApiService.GetTournamentAsync(
                                tournamentIdCandidate,
                                apiKey
                            );
                            var latestMatch = candidateTournament?.Included?
                            .Where(x => x.Type == "match")
                            .OrderByDescending(x => x.Attributes?.CreatedAt)
                            .FirstOrDefault();

                            _logger.LogInformation(
                            "Candidate={TournamentId}, LatestMatchCreatedAt={CreatedAt}",
                            tournamentIdCandidate,
                            latestMatch?.Attributes?.CreatedAt);

                            var earliestMatch = candidateTournament?.Included?
                            .Where(x => x.Type == "match")
                            .OrderBy(x => x.Attributes?.CreatedAt)
                            .FirstOrDefault();

                            _logger.LogInformation(
                            "Candidate={TournamentId}, EarliestMatchCreatedAt={CreatedAt}",
                            tournamentIdCandidate,
                            earliestMatch?.Attributes?.CreatedAt);

                            _logger.LogInformation(
                                "[DRY RUN] {TournamentId} | EarliestKind={EarliestKind}, LatestKind={LatestKind}",
                                tournamentIdCandidate,
                                earliestMatch?.Attributes?.CreatedAt.Kind,
                                latestMatch?.Attributes?.CreatedAt.Kind
                            );
                        

                            int matchCount = candidateTournament?.Included?
                                .Count(x => x.Type == "match") ?? 0;

                            _logger.LogInformation(
                                "[DRY RUN] Tournament {TournamentId} has {MatchCount} matches",
                                tournamentIdCandidate,
                                matchCount
                            );
                            var candidateMatchIds = candidateTournament?.Included?
                            .Where(x => x.Type == "match")
                            .Select(x => x.Id)
                            .ToHashSet() ?? new HashSet<string>();

                        var sentCandidateMatchIds = await dbContext.SentMatches
                            .Where(x => x.TournamentId == tournamentIdCandidate)
                            .Select(x => x.MatchId)
                            .ToHashSetAsync(stoppingToken);
                        
                        int eligiblePendingCount = candidateTournament?.Included?
                        .Count(x =>
                            x.Type == "match" &&
                            IsWithinProcessingWindow(
                                x.Attributes?.CreatedAt,
                                processingNotBeforeUtc
                            ) &&
                            !sentCandidateMatchIds.Contains(x.Id)
                        ) ?? 0;

                        _logger.LogInformation(
                            "[DRY RUN] {TournamentId} | Eligible Pending: {Count}",
                            tournamentIdCandidate,
                            eligiblePendingCount
                        );
                                                
                        int pendingCount = candidateMatchIds.Count(
                            id => !sentCandidateMatchIds.Contains(id)
                        );

                        _logger.LogInformation(
                            "[DRY RUN] {TournamentId} | Pending matches: {Count}",
                            tournamentIdCandidate,
                            pendingCount
                        );
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "[DRY RUN] Failed to fetch tournament {TournamentId}",
                                tournamentIdCandidate
                            );
                        }
                        catch (OperationCanceledException ex)
                            when (!stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogWarning(
                                ex,
                                "[DRY RUN] Tournament {TournamentId} request timed out",
                                tournamentIdCandidate
                            );
                        }
                    }
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken
                );
                continue;
            }
            if (DateTimeOffset.UtcNow < nextMatchAt)
            {
                await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken
                );

                continue;
            }

            nextMatchAt = DateTimeOffset.UtcNow.AddMinutes(2);  
            foreach (var tournamentIdCandidate in candidateTournamentIds)
            {
                try
                {
                    await ProcessTournamentAsync(
                        tournamentIdCandidate,
                        apiKey,
                        processingNotBeforeUtc,
                        stoppingToken
                    );
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to process tournament {TournamentId}",
                        tournamentIdCandidate
                    );
                }
                catch (OperationCanceledException ex)
                    when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        ex,
                        "Tournament {TournamentId} processing timed out or was canceled unexpectedly",
                        tournamentIdCandidate
                    );
                }
                catch (OperationCanceledException ex)
                    when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        ex,
                        "Tournament Discovery timed out or was canceled unexpectedly"
                    );
                }
            }
            
            
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken
            );
        }
    }
    private async Task ProcessTournamentAsync(
    string tournamentId,
    string apiKey,
    DateTimeOffset processingNotBeforeUtc,
    CancellationToken stoppingToken)
    {
    using var scope = _scopeFactory.CreateScope();
    bool processingDryRun =
            _configuration.GetValue<bool>("Pubg:ProcessingDryRun");
    if (!processingDryRun &&
    !_configuration.GetValue<bool>("Pubg:LiveSendEnabled"))
    {
        throw new InvalidOperationException(
            "Live processing is disabled."
        );
    }

    var dbContext = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    var externalApiService = scope.ServiceProvider
        .GetRequiredService<ExternalApiService>();
    var tournament =
            await externalApiService.GetTournamentAsync(tournamentId,apiKey);

            if (tournament == null)
            {
                _logger.LogWarning(
                    "Tournament {TournamentId} not found",
                    tournamentId
                );

                return;
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

                    if (!processingDryRun)
                    {
                        foreach (var standing in standings.Values)
                        {
                            if (standing.Id == 0)
                            {
                                dbContext.TeamStandings.Add(standing);
                            }
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }

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
                if (!IsWithinProcessingWindow(
                    currentMatch.Attributes?.CreatedAt,
                    processingNotBeforeUtc))
                {
                    _logger.LogInformation(
                        "Skipping Match {MatchId}: Invalid timestamp or before cutoff",
                        currentMatch.Id
                    );

                    continue;
                }
                bool alreadySent =
                            await dbContext.SentMatches
                                .AnyAsync(
                                    x => x.TournamentId == tournamentId
                                    && x.MatchId == currentMatch.Id,
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

                    if (processingDryRun)
                    {
                        _logger.LogInformation(
                            "[PROCESSING DRY RUN] Match {MatchNumber} | Length: {Length}",
                            matchResult.MatchNumber,
                            message.Length
                        );

                        _standingsCalculator.ApplyMatch(
                            standings,
                            matchResult,
                            tournamentId
                        );

                        continue;
                    }
                   
                    var discordWebhookService =
                    scope.ServiceProvider
                        .GetRequiredService<DiscordWebhookService>();

                    string webhookUrl =
                    _configuration["Discord:WebhookUrl"]
                    ?? throw new InvalidOperationException(
                        "Discord webhook is not configured"
                    );    

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
                
                    
                    if (processingDryRun)
                    {
                        _logger.LogInformation(
                            "[DRY RUN FINAL STANDINGS] {TournamentId}",
                            tournamentId
                        );

                        foreach (var team in standings.Values
                            .OrderByDescending(x => x.TotalPoints))
                        {
                            _logger.LogInformation(
                                "{Team} | MP: {MP} | Kills: {Kills} | Total: {Total}",
                                team.TeamTag,
                                team.MatchesPlayed,
                                team.TotalKills,
                                team.TotalPoints
                            );
                        }
                    }
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
    private static bool IsWithinProcessingWindow(
    DateTime? createdAt,
    DateTimeOffset cutoff)

    {
    return createdAt.HasValue
        && createdAt.Value.Kind == DateTimeKind.Utc
        && createdAt.Value >= cutoff.UtcDateTime;
    }

}

        
