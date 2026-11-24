using ServicePractice.Services;
using Microsoft.EntityFrameworkCore;
using ServicePractice.Data;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ExternalApiService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=pubg-notifier.db"));
builder.Services.AddScoped<DiscordWebhookService>();
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddSingleton<MatchResultFormatter>();
builder.Services.AddSingleton<ScoringCalculator>();
builder.Services.AddSingleton<StandingsCalculator>();
builder.Services.AddScoped<TournamentDiscoveryService>();
var app = builder.Build();

//testing auto discovery tournamentId

if (args.Contains("--test-discovery"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("TournamentDiscoveryTest");

    var discovery = scope.ServiceProvider
        .GetRequiredService<TournamentDiscoveryService>();

    string apiKey = app.Configuration["Pubg:ApiKey"]
        ?? throw new InvalidOperationException("API Key missing");

    string eventName =
        app.Configuration["Pubg:TournamentSearch:Event"]
        ?? throw new InvalidOperationException("Event missing");

    int year = app.Configuration.GetValue<int>(
        "Pubg:TournamentSearch:Year");

    
using var cts = new CancellationTokenSource();

ConsoleCancelEventHandler cancelHandler = (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.CancelKeyPress += cancelHandler;

try
{
    using var timer = new PeriodicTimer(
        TimeSpan.FromSeconds(10)
    );

    for (int poll = 1; poll <= 3; poll++)
    {
        if (cts.IsCancellationRequested)
            break;

        try
        {
            var tournaments = await discovery.FindCandidatesAsync(
                apiKey,
                eventName,
                year,
                cts.Token
            );

            logger.LogInformation(
                "Poll {Poll}/{Total} | Candidates: {Count}",
                poll,
                3,
                tournaments.Count
            );

            foreach (var tournament in tournaments)
            {
                logger.LogInformation(
                    "Candidate: {TournamentId}",
                    tournament.Id
                );
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
            ex,
            "Tournament Discovery failed at Poll {Poll}",
            poll
    );
        }

        if (poll < 3)
        {
            await timer.WaitForNextTickAsync(cts.Token);
        }
    }
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.WriteLine("Polling cancelled.");
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

return;

}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
