---
title: Query Cratis Chronicle behavior patterns in .NET
description: Query mined behavior in Cratis Chronicle from .NET to suggest usual actions, check established context, or browse the patterns held for a user.
---

Cratis Chronicle's .NET [behavior patterns](/chronicle/patterns/) API lets you ask what a user usually does in a given context, based on mined event history. Use `IEventStore.Patterns` to suggest a usual action, check whether an action fits an established pattern, or browse what the store has learned. These are observations about recorded behavior, not guarantees about what someone will do next.

## Asking what somebody usually does right now

Call `GetPatternsAt` when you want to suggest what someone usually does at this point in the week. Pass a scope, the grouping key whose behavior you want to query, normally the user's identity subject. With no moment supplied, the method uses the current time. See [asking about a moment](/chronicle/patterns/) for how the query uses that time.

```csharp
foreach (var pattern in await eventStore.Patterns.GetPatternsAt(userId))
{
    var command = pattern.Facets.ValueOf(FacetName.CommandType);
    Console.WriteLine($"{command} — {pattern.Confidence.Value:P0} of the time");
}
```

Each answer names an action in `CommandType`. Chronicle takes it from the last named link in the causation chain, the record of what led to the append. If no link names itself, Chronicle uses the event type. `Confidence` measures how often the action occurs in the context where the pattern was established. The query returns at most one answer per action, even if mining found it at several levels of detail.

`GetPatternsAt` derives the day and time bucket from the moment using the engine's bucketing rule. Pass a moment to ask about another time:

```csharp
var patterns = await eventStore.Patterns.GetPatternsAt(userId, tomorrowMorning);
```

Add facets, the named dimensions of a context, when you need a narrower question. For example, constrain the event source type to ask what the user usually does with an invoice at this time:

```csharp
var patterns = await eventStore.Patterns.GetPatternsAt(
    userId,
    alsoConstraining: FacetSet.Empty.With(FacetName.AggregateType, "Invoice"));
```

Use `GetPatternsAt` rather than duplicating the time-bucketing rules in your application. A different rule can produce an empty result simply because it asks about a different slot. If you need the bucket separately, `ToTimeBucket()` on `DateTimeOffset` uses the same rule as the engine and reads the offset carried by the moment.

## Asking about a context that is not a moment

Use `GetUsualActions` when the known context is not tied to a particular moment. Build a `FacetSet` from the dimensions you know, then ask within the user's scope:

```csharp
using Cratis.Chronicle.Concepts.Patterns;

var patterns = await eventStore.Patterns.GetUsualActions(
    groupingKey: userId,
    context: FacetSet.Empty
        .With(FacetName.Day, DayOfWeek.Monday.ToString())
        .With(FacetName.AggregateType, "Invoice"));

foreach (var pattern in patterns)
{
    Console.WriteLine($"{pattern.Facets.ValueOf(FacetName.CommandType)} — {pattern.Confidence.Value:P0} confident, seen {pattern.Occurrences.Value} times");
}
```

You can supply any subset of the facets. The server discards facets it is not configured to mine, so including one does not narrow the results.

Results are ranked by descending confidence, then descending context specificity, then descending support, with ties broken by facet key in ascending ordinal order. `CommandType` does not constrain this query because the action is the answer you are asking for, not part of the situation you already know.

## Checking whether an action is normal

Use `GetPatterns` when you already know the action and want to check which established patterns cover it in this context. Include `CommandType` with the dimensions you want to check. This can inform an unusual-action check, but an empty result alone does not establish that the action is wrong.

```csharp
var covering = await eventStore.Patterns.GetPatterns(
    groupingKey: userId,
    context: FacetSet.Empty
        .With(FacetName.CommandType, "DeleteSupplier")
        .With(FacetName.Day, DayOfWeek.Sunday.ToString())
        .With(FacetName.TimeBucket, TimeBucket.Night.ToString()));

if (!covering.Any(pattern => pattern.Facets.Constrains(FacetName.CommandType)))
{
    // Nothing established covers this person doing this, here.
}
```

A pattern matches when all of its facets are present with the same values in your context. Results are ranked most specific first, then most confident, so a detailed match appears ahead of a broader one. The example checks for an action facet because a result that only describes the day or time does not establish that this action is normal.

### An empty result is an answer

Both calls return an empty collection when no stored pattern meets the context and confidence threshold. That can mean the scope has too little recorded history or that its patterns do not cover this situation. It does not mean the action never happens. Let the application say it does not know rather than inventing a recommendation:

```csharp
var patterns = await eventStore.Patterns.GetUsualActions(userId, context);
if (!patterns.Any())
{
    return "I don't have enough history to say what usually happens here.";
}
```

### Thresholds and limits

Set `minimumConfidence` when your application needs a higher confidence bar, and `maximumResults` to bound the number of answers:

```csharp
var patterns = await eventStore.Patterns.GetUsualActions(
    groupingKey: userId,
    context: context,
    minimumConfidence: new PatternConfidence(0.8d),
    maximumResults: 3,
    cancellationToken: cancellationToken);
```

If you omit `minimumConfidence`, the server uses its configured minimum. If you omit `maximumResults`, the server returns at most ten results. The client sends these choices to the server rather than filtering the response locally.

## Browsing everything a scope established

Use `GetPatternsForScope` to build a browsing view rather than answer a question about one situation. It returns all stored patterns for a scope, including those below the confidence threshold used by the query methods:

```csharp
var everything = await eventStore.Patterns.GetPatternsForScope(userId);
```

If the view needs a scope selector, call `GetScopes` to list the scopes that hold patterns:

```csharp
foreach (var scope in await eventStore.Patterns.GetScopes())
{
    var patterns = await eventStore.Patterns.GetPatternsForScope(scope);
}
```

This list is not an identity directory. A scope missing from it has no stored patterns to browse, even if its identity has appeared in the event store.

## What you get back

Each result is a `BehaviorPattern`:

| Member | Type | Meaning |
| --- | --- | --- |
| `GroupingKey` | `PatternGroupingKey` | The scope the pattern belongs to |
| `Facets` | `FacetSet` | The facets the pattern constrains |
| `Occurrences` | `PatternOccurrences` | The miner's count of observations of this pattern |
| `Confidence` | `PatternConfidence` | How often it holds when its context is present, 0 to 1 |
| `Support` | `PatternSupport` | The share of observed events in this scope it was seen in, 0 to 1 |
| `Weight` | `PatternWeight` | Recency-weighted strength — decays as the behavior goes unseen |
| `FirstSeen` / `LastSeen` | `DateTimeOffset` | When it was first and last observed |

`Occurrences` is a mining count, not an exact audit total; checkpoint replay after a crash can cause a small tail of events to be counted again. `Weight` reflects recency rather than the total count. Order a browsing view by `Weight` when recent behavior matters more than older observations.

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

`FacetSet` is immutable and normalizes its contents: facets are ordered by name, each name has at most one value, and unspecified values are dropped. `With` returns a new set and replaces any existing value for that name. In the example, the second `Day` assignment wins, so the resulting context describes Tuesday, not Monday:

```csharp
var context = FacetSet.Empty
    .With(FacetName.CommandType, "ApproveExpenseReport")
    .With(FacetName.Day, DayOfWeek.Monday.ToString())
    .With(FacetName.Day, DayOfWeek.Tuesday.ToString());   // replaces Monday
```

The well-known names on `FacetName` are `CommandType`, `InitiatorType`, `InitiatorId`, `OnBehalfOf`, `CausedByCommand`, `CorrelationRootId`, `AggregateType`, `Year`, `Month`, `Day`, and `TimeBucket`. See [Behavior Patterns](/chronicle/patterns/) to choose the facets that answer your application's question and check which ones the store mines by default.

## Making the command visible to mining

For patterns to name a command rather than fall back to an event type, record the command in the causation chain. The [Cratis Arc](/arc/) command pipeline does this through its Chronicle integration, recording a `Command` causation for the executing command.

When appending directly, open a causation scope around the work and set the command name:

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

Use `BeginScope` for work with a beginning and an end. Disposing the scope removes its causation from subsequent appends. `Add` leaves the link on the chain, so using it for two consecutive commands can make the second appear to have been caused by the first. A bounded scope keeps the recorded relationship aligned with the work that actually ran.
