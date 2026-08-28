---
title: Querying behavior patterns
description: Ask what a user usually does in a given context, from the .NET client, backed by the patterns Chronicle mined from event history.
---

[Behavior patterns](/chronicle/patterns/) are recurring combinations of context that Chronicle mined from an event store's history. The .NET client exposes them on `IEventStore.Patterns`.

## Asking what usually happens

Build a context from the facets you know and ask within a scope — normally the user whose behavior you are asking about:

```csharp
using Cratis.Chronicle.Concepts.Patterns;

var patterns = await eventStore.Patterns.GetPatterns(
    groupingKey: userId,
    context: FacetSet.Empty
        .With(FacetName.Day, DayOfWeek.Monday.ToString())
        .With(FacetName.TimeBucket, TimeBucket.Morning.ToString()));

foreach (var pattern in patterns)
{
    Console.WriteLine($"{pattern.Facets} — {pattern.Confidence.Value:P0} confident, seen {pattern.Occurrences.Value} times");
}
```

The context may constrain **any subset** of the facets. Facets the store does not mine are discarded rather than narrowing the lookup to nothing, so a caller can describe its situation in whatever terms it has.

Results are ranked **most specific first**, then most confident. A pattern constraining everything you asked about answers your question; a broader, more confident one answers a question you did not ask.

### An empty result is an answer

`GetPatterns` returns nothing when no pattern clears the confidence bar. That is a true statement — this scope has no established behavior for this context — and it is deliberately not padded with the best of a bad set. Treat "no patterns" as "do not claim to know":

```csharp
var patterns = await eventStore.Patterns.GetPatterns(userId, context);
if (!patterns.Any())
{
    return "I don't have enough history to say what usually happens here.";
}
```

### Thresholds and limits

```csharp
var patterns = await eventStore.Patterns.GetPatterns(
    groupingKey: userId,
    context: context,
    minimumConfidence: new PatternConfidence(0.8d),
    maximumResults: 3,
    cancellationToken: cancellationToken);
```

Leave `minimumConfidence` and `maximumResults` unset to use whatever the **server** is configured for. The client deliberately does not carry its own copy of the thresholds — a default duplicated here would silently disagree with the configured one the moment either changed.

## Browsing everything a scope established

`GetPatternsForScope` is the listing call — everything held for a scope, unfiltered, including patterns below the confidence threshold. It is what a browsing view binds to, as opposed to `GetPatterns`, which answers a question about one situation.

```csharp
var everything = await eventStore.Patterns.GetPatternsForScope(userId);
```

## What you get back

Each result is a `BehaviorPattern`:

| Member | Type | Meaning |
| --- | --- | --- |
| `GroupingKey` | `PatternGroupingKey` | The scope the pattern belongs to |
| `Facets` | `FacetSet` | The facets the pattern constrains |
| `Occurrences` | `PatternOccurrences` | How many times it has been observed |
| `Confidence` | `PatternConfidence` | How often it holds when its context is present, 0 to 1 |
| `Support` | `PatternSupport` | The share of all observed events it was seen in, 0 to 1 |
| `Weight` | `PatternWeight` | Recency-weighted strength — decays as the behavior goes unseen |
| `FirstSeen` / `LastSeen` | `DateTimeOffset` | When it was first and last observed |

`Occurrences` counts everything that ever happened; `Weight` is how much of that is still recent. Order by `Weight` when recency matters more than history.

Read a single facet off a pattern with `ValueOf`, and check whether it constrains one at all with `Constrains`:

```csharp
var pattern = patterns.First();
var command = pattern.Facets.ValueOf(FacetName.CommandType);

if (pattern.Facets.Constrains(FacetName.TimeBucket))
{
    // The pattern is specific to a part of the day.
}
```

## Building a context

`FacetSet` is canonical and immutable: facets are ordered by name, at most one value per facet survives, and facets with no value are dropped. `With` returns a new set and **replaces** a facet that is already constrained, so a context built up in steps never depends on the order it was written in.

```csharp
var context = FacetSet.Empty
    .With(FacetName.CommandType, "ApproveExpenseReport")
    .With(FacetName.Day, DayOfWeek.Monday.ToString())
    .With(FacetName.Day, DayOfWeek.Tuesday.ToString());   // replaces Monday
```

The facet names are the well-known statics on `FacetName` — `CommandType`, `InitiatorType`, `InitiatorId`, `OnBehalfOf`, `CausedByCommand`, `CorrelationRootId`, `AggregateType`, `Year`, `Month`, `Day` and `TimeBucket`. See [Behavior Patterns](/chronicle/patterns/) for what each one means and which of them are mined by default.

## Making the command visible to mining

`CommandType` and `CausedByCommand` are only as good as what named the command. If you append through [Cratis Arc](/arc/), its command pipeline records a `Command` causation naming the executing command and nothing else is needed.

Appending directly, add the same property to the causation you scope around the work:

```csharp
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

using (causationManager.BeginScope(
    "Command",
    new Dictionary<string, string> { { WellKnownCausationProperties.CommandType, nameof(ApproveExpenseReport) } }))
{
    await eventStore.EventLog.Append(expenseReportId, new ExpenseReportApproved(approvedBy));
}
```

Use `BeginScope` rather than `Add` for work with a beginning and an end. `Add` is append-only, which is right for a link describing how the work arrived — an HTTP request — but wrong for one describing a bounded piece of work: two such pieces done one after the other both stay on the chain, and the second then reads as caused by the first. That ordering never happened, and pattern mining would learn it as a fact.
