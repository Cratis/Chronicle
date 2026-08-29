// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Identities;
using Microsoft.Extensions.Logging;
using Samples.ExpenseApprovals;

const int Weeks = 26;
const int Seed = 20260829;

using var loggerFactory = LoggerFactory.Create(static builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

// A local kernel serves its dev certificate, which the client rejects unless told not to validate it. Overridable
// so the sample can be pointed at a real deployment without editing it.
var connectionString = Environment.GetEnvironmentVariable("CHRONICLE_CONNECTION_STRING")
    ?? "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000?skipTlsValidation=true";

var identityProvider = new BaseIdentityProvider();
var options = ChronicleOptions.FromConnectionString(connectionString);

Console.WriteLine("Connecting to Chronicle...");
using var client = new ChronicleClient(options, identityProvider: identityProvider, loggerFactory: loggerFactory);
var store = await client.GetEventStore("ExpenseApprovals");
var appender = new ActivityAppender(store, identityProvider, client.CausationManager);

// Every command is reachable as an argument as well as a keystroke, so the sample can be driven from a script -
// seeding a demo environment should not need somebody sitting at the keyboard.
if (args.Length > 0)
{
    switch (args[0].ToLowerInvariant())
    {
        case "generate":
            await GenerateHistory();
            return 0;
        case "scopes":
            await ListScopes();
            return 0;
        case "patterns":
            await ShowPatterns(ScopeFromArguments());
            return 0;
        case "now":
            await PredictNow(ScopeFromArguments());
            return 0;
        default:
            Console.WriteLine($"Unknown command '{args[0]}'. Expected generate, scopes, patterns or now.");
            return 1;
    }
}

WriteInstructions();

while (true)
{
    switch (Console.ReadKey(true).Key)
    {
        case ConsoleKey.G:
            await GenerateHistory();
            break;
        case ConsoleKey.S:
            await ListScopes();
            break;
        case ConsoleKey.P:
            await ShowPatterns(await SelectScope());
            break;
        case ConsoleKey.N:
            await PredictNow(await SelectScope());
            break;
        case ConsoleKey.H:
            WriteInstructions();
            break;
        case ConsoleKey.Q:
        case ConsoleKey.Escape:
            return 0;
    }
}

async Task GenerateHistory()
{
    Console.WriteLine($"Generating {Weeks} weeks of expense activity. This appends thousands of events one at a time - give it a minute.");

    var lastReported = 0;
    var result = await SampleHistory.Generate(appender, Weeks, Seed, events =>
    {
        if (events - lastReported < 500)
        {
            return;
        }

        lastReported = events;
        Console.WriteLine($"  {events} events...");
    });

    Console.WriteLine($"Appended {result.Events} events across {result.Reports} claims. Skipped {result.Skipped} that were already there.");
    Console.WriteLine("Patterns appear once the observer has worked through the history and enough behavior clears the thresholds.");
}

async Task ListScopes()
{
    var scopes = (await store.Patterns.GetScopes()).ToArray();

    if (scopes.Length == 0)
    {
        Console.WriteLine("No scope has established behavior yet.");
        return;
    }

    Console.WriteLine("\nScopes with established behavior:");
    for (var index = 0; index < scopes.Length; index++)
    {
        Console.WriteLine($"  [{index + 1}] {scopes[index]}");
    }
}

async Task ShowPatterns(PatternGroupingKey? scope)
{
    if (scope is null)
    {
        return;
    }

    var patterns = (await store.Patterns.GetPatternsForScope(scope))
        .OrderByDescending(pattern => pattern.Confidence.Value)
        .ToArray();

    Console.WriteLine($"\n{patterns.Length} patterns for {scope}:");
    foreach (var pattern in patterns)
    {
        Console.WriteLine($"  {pattern.Confidence.Value,6:P0}  {pattern.Occurrences.Value,5}x  {pattern.Facets}");
    }
}

// Asks what usually happens right now - the same question the heatmap's "right now" panel asks.
async Task PredictNow(PatternGroupingKey? scope)
{
    if (scope is null)
    {
        return;
    }

    var now = DateTimeOffset.Now;
    var context = FacetSet.Empty
        .With(FacetName.Day, now.DayOfWeek.ToString())
        .With(FacetName.TimeBucket, TimeBucketFor(now.Hour));

    var patterns = (await store.Patterns.GetPatterns(scope, context)).ToArray();

    Console.WriteLine($"\n{now.DayOfWeek}, {TimeBucketFor(now.Hour)} - for {scope}:");

    if (patterns.Length == 0)
    {
        Console.WriteLine("  Nothing established for this moment. That is an answer, not a gap.");
        return;
    }

    foreach (var pattern in patterns)
    {
        Console.WriteLine($"  {pattern.Confidence.Value,6:P0}  {pattern.Facets}");
    }
}

PatternGroupingKey? ScopeFromArguments()
{
    if (args.Length > 1)
    {
        return new PatternGroupingKey(args[1]);
    }

    Console.WriteLine("Name the scope to ask about, for example: dana.reeves");
    return null;
}

async Task<PatternGroupingKey?> SelectScope()
{
    var scopes = (await store.Patterns.GetScopes()).ToArray();

    if (scopes.Length == 0)
    {
        Console.WriteLine("No scope has established behavior yet. Generate the history with G first.");
        return null;
    }

    for (var index = 0; index < scopes.Length; index++)
    {
        Console.WriteLine($"  [{index + 1}] {scopes[index]}");
    }

    Console.Write("Pick a scope: ");
    var choice = Console.ReadKey().KeyChar - '1';
    Console.WriteLine();

    if (choice < 0 || choice >= scopes.Length)
    {
        Console.WriteLine("No such scope.");
        return null;
    }

    return scopes[choice];
}

// Mirrors the kernel's own bucketing so the question asked here lands in the bucket the patterns were mined under.
static string TimeBucketFor(int hour) => hour switch
{
    >= 5 and < 8 => nameof(TimeBucket.EarlyMorning),
    >= 8 and < 11 => nameof(TimeBucket.Morning),
    >= 11 and < 14 => nameof(TimeBucket.Midday),
    >= 14 and < 17 => nameof(TimeBucket.Afternoon),
    >= 17 and < 22 => nameof(TimeBucket.Evening),
    _ => nameof(TimeBucket.Night)
};

void WriteInstructions()
{
    string[] lines =
    [
        string.Empty,
        "Expense approvals - a store with recurring behavior baked into it.",
        string.Empty,
        "  G = Generate the backdated history",
        "  S = List the scopes with established behavior",
        "  P = Show every pattern for a scope",
        "  N = Ask what usually happens right now",
        "  H = Show this menu             Q = Quit",
        string.Empty
    ];

    foreach (var line in lines)
    {
        Console.WriteLine(line);
    }
}
