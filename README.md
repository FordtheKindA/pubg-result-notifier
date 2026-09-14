# PUBG Result Notifier

A .NET background service that monitors PUBG tournament matches and automatically sends formatted match results to Discord.

The project was created for situations where I cannot watch a tournament live but still want to follow match results without checking livestream chats or other sources that may contain spoilers.

## Features

- Polls PUBG Tournament API using `BackgroundService`
- Detects newly completed matches
- Fetches detailed match data from the PUBG API
- Joins roster and participant data to calculate team results
- Displays team placement and total kills
- Converts PUBG map codes into readable map names
- Assigns match numbers based on match creation time
- Converts match time to GMT+7
- Sends multiple newly detected matches to Discord in a single batch
- Stores sent Match IDs in SQLite to prevent duplicate notifications
- Persists deduplication data across application restarts
- Stores API credentials using .NET User Secrets instead of hardcoding them in source code

## Tech Stack

- C#
- .NET 9
- ASP.NET Core
- BackgroundService
- Entity Framework Core
- SQLite
- HttpClient
- PUBG API
- Discord Webhook

## How It Works

```text
PUBG Tournament API
        |
        v
BackgroundService Polling
        |
        v
Detect New Matches
        |
        v
Fetch Match Details
        |
        v
Roster + Participant Join
        |
        v
Build MatchResult / TeamResult
        |
        v
Format Match Results
        |
        v
Send Batch to Discord
        |
        v
Save Match IDs to SQLite
```

The worker periodically requests tournament data from the PUBG API.

Matches are ordered by their `CreatedAt` value and assigned a match number.

For each match that has not already been sent, the application retrieves detailed match information. PUBG roster and participant data are joined to calculate each team's placement and total kills.

New match results are collected into a batch and formatted before being sent to Discord.

Only after the Discord request succeeds are the Match IDs stored in SQLite. This prevents a failed Discord request from incorrectly marking a match as already sent.

## Duplicate Prevention

Earlier versions stored sent Match IDs in memory.

This caused old matches to be sent again whenever the application restarted.

The current version stores sent Match IDs in SQLite:

```text
SentMatches
- Id
- TournamentId
- MatchId
- SentAt
```

Before processing a match, the worker checks whether its `MatchId` already exists.

If it exists, the match is skipped.

## Configuration

This project uses .NET User Secrets for local credentials.

Initialize User Secrets:

```bash
dotnet user-secrets init
```

Set the PUBG API key:

```bash
dotnet user-secrets set "Pubg:ApiKey" "YOUR_PUBG_API_KEY"
```

Set the Discord webhook:

```bash
dotnet user-secrets set "Discord:WebhookUrl" "YOUR_DISCORD_WEBHOOK_URL"
```

Credentials are not stored in the Git repository.

## Database

The project uses SQLite with Entity Framework Core.

Create or update the database:

```bash
dotnet ef database update
```

The local database file is excluded from Git.

## Run

Restore dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run:

```bash
dotnet run
```

## Current Limitations

- Tournament ID is currently configured in the application
- The project currently focuses on tournament match result notifications
- No frontend or management UI is included
- Discord webhook and PUBG API credentials must be configured locally

## Future Improvements

Possible future improvements include:

- Configurable tournament selection
- Tournament standings calculation
- Placement points and kill points
- Improved Discord message formatting
- Unit tests for formatting and result calculation
- Deployment as a continuously running service

## Project Background

This project was built as a backend portfolio project to practice and apply:

- Working with external APIs
- JSON deserialization
- Background processing
- Dependency Injection
- Service lifetimes
- Entity Framework Core
- Persistent deduplication
- Async/Await
- Error handling
- Separation between API models, application models, formatting, and notification services