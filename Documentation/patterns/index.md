---
title: "Cratis Chronicle behavior patterns: understand recurring actions"
description: Cratis Chronicle behavior patterns summarize recurring actions in event context so you can ask what a user usually does in a given situation.
---

<a id="behavior-patterns"></a>

Cratis Chronicle behavior patterns summarize recurring actions in event context so you can ask what a user usually does in a given situation. For example, an application can ask which commands a person usually carries out on Monday mornings, using the history recorded for that person.

Use the results to suggest a familiar action in your application, give an agent context about established routines, or inspect those routines in the Workbench. Patterns describe observed behavior; they do not establish what someone should do next. An empty result means no pattern meets the query's conditions, not that an action is wrong.

## What a pattern is

A behavior pattern is a recurring combination of facets: named dimensions of an event's context, such as the command, day of the week, or part of the day. Chronicle retains combinations that meet its support and confidence thresholds.

| Part | Meaning |
| --- | --- |
| Grouping key | The scope the behavior belongs to, normally a user |
| Facets | The context dimensions and values the pattern describes |
| Occurrences | The approximate count of observed events containing the combination |
| Confidence | How often the action occurs within the pattern's context, from 0 to 1 |
| Support | The share of observed events in this scope containing the combination, from 0 to 1 |
| Weight | A recency-weighted measure that decreases with elapsed time between observations |
| First seen / last seen | When the combination was first and last observed |

For a pattern such as `{ Day: Monday, TimeBucket: EarlyMorning, CommandType: RegisterInvoice }`, confidence of `0.9` means that about 90% of this scope's observed events in that day-and-time context carry that action. It does not mean the person registers invoices on 90% of Mondays: the miner counts events, not calendar days or command invocations.

## Facets

Chronicle reads facets from an event's context, not its payload. A pattern can describe who acted, what caused the event, and when it occurred without depending on fields specific to an invoice or a leave request.

| Facet | Where it comes from |
| --- | --- |
| `CommandType` | The command that produced the event; the event type when nothing above it named itself |
| `InitiatorType` | `User`, `Agent`, `System` or `Unknown` |
| `InitiatorId` | The identity that caused the event |
| `OnBehalfOf` | The identity it acted for, when it acted for someone else |
| `CausedByCommand` | The command one level up the causation chain |
| `CorrelationRootId` | The correlation the event belongs to |
| `AggregateType` | The event source type the event was appended to |
| `Year`, `Month` | Taken from the event's occurred timestamp |
| `Day` | Day of week |
| `TimeBucket` | `EarlyMorning`, `Morning`, `Midday`, `Afternoon`, `Evening` or `Night` |

Time-derived facets use the event's own occurred timestamp, including its offset, rather than the time Chronicle processes it. A backdated append therefore contributes to the day and time bucket recorded on the event. The pattern capture observer is not replayable; replay is not a way to rebuild these counts.

### Who the behavior belongs to

Chronicle groups behavior by the identity at the root of the on-behalf-of chain. An agent acting for a person therefore contributes to that person's scope. `InitiatorType` still distinguishes agent-driven work from work the person does directly.

Events without an identifiable root identity are not mined. They do not contribute to a shared anonymous scope. Scopes are also separate for each event store and namespace, so the same identity name in two namespaces does not combine their histories.

## How mining works

The miner keeps a Lossy Counting sketch per scope: an in-memory summary of candidate combinations and their counts, rather than a separate mining record for every event. It expands each event's selected facets into combinations of up to `MaximumCombinationSize` facets, which defaults to 3. Limiting the combination size controls how many candidates each event contributes.

Lossy Counting periodically removes combinations that have not remained frequent enough. The `Error` setting controls the counting approximation and its memory trade-off. Only combinations meeting both `MinimumSupport` and `MinimumConfidence` are persisted as patterns, so pattern storage contains surviving summaries rather than a copy of the event history.

Weight is separate from these thresholds. When a combination occurs again, its previous weight is reduced according to `DecayFactor` and the elapsed time before the new occurrence is added. A lower weight does not by itself remove a pattern: support and confidence use occurrence counts.

Mining updates the sketch as events are observed. Persistence happens later, on the `PersistenceInterval` cadence, and rewrites the surviving patterns for scopes touched during that interval. Allow for observer processing and this persistence delay before expecting a newly appended event to affect a query.

### Across restarts

Before mining new events for a scope after a restart, Chronicle restores its persisted patterns into the sketch. This preserves established behavior instead of treating the first few new events as the scope's entire history.

`Occurrences` remains approximate. Lossy Counting can undercount combinations, and events after the observer's last checkpoint can be mined again after a crash. Use the event log when you need an exact tally.

### Confidence

For a combination containing `CommandType` and context facets, confidence is the combination's occurrence count divided by the count for the same context without `CommandType`. This answers how common the action is within that context. A combination with no action, or with an action but no context facets, uses support as its confidence.

## Configuration

Configure mining under `Cratis:Chronicle:PatternDetection`. Start with the default facets, then add dimensions only when they help answer your application's question.

| Setting | Default | What it does |
| --- | --- | --- |
| `Facets` | `CommandType`, `InitiatorType`, `CausedByCommand`, `AggregateType`, `Day`, `TimeBucket` | Which facets take part in the mined combination |
| `MaximumCombinationSize` | `3` | The largest number of facets a combination may hold |
| `Error` | `0.001` | The Lossy Counting error parameter; a smaller value trades more memory for counting accuracy |
| `MinimumSupport` | `0.01` | The smallest share of events a combination must hold to survive |
| `MinimumConfidence` | `0.5` | The smallest confidence a combination must hold to survive |
| `DecayFactor` | `0.99` | Daily decay factor applied to a combination's weight between observations |
| `PersistenceInterval` | `5` | Seconds between persisting the scopes mining has touched |

`Year` and `Month` are available but excluded from `Facets` by default. Including them splits otherwise recurring behavior across separate years or months and increases the number of distinct combinations to track. Add them when that seasonal distinction matters. `FirstSeen` and `LastSeen` retain the pattern's observed time range without adding those dimensions.

## When capture starts

Pattern capture subscribes to the event types registered in an event store and updates its subscriptions as more types are registered. It also subscribes namespaces added while the server is running. Neither a new event type nor a new namespace requires a restart to begin capture.

## Naming the command

To distinguish commands in patterns, record their names in the causation chain: the context links describing what led to an event. The Chronicle integration in [Cratis Arc](/arc/) records a `Command` causation for the duration of each command. Nested commands add another link; commands executed one after another do not become each other's cause. Applications appending directly to Chronicle can supply a `commandType` property on their causation.

The miner uses the last named causation link for `CommandType` and the preceding named link for `CausedByCommand`. When no link names itself, `CommandType` falls back to the event type. Without command metadata, do not interpret that fallback as the name of a command that ran.

For an ASP.NET Core request, mining uses the matched route **template** (for example, `/customers/{email}`), not the request path (such as `/customers/jane@example.com`). The causation middleware still records the actual path for other consumers, but the miner ignores it. When no route template is available, the request is named by its causation type instead. Place the middleware after endpoint routing if request templates should be available to it.

Older persisted patterns may contain actual request paths. Upgrading does not remove those historical values; review stored patterns for cleanup where needed.

## Querying patterns

Choose the query according to whether you need an action suggestion, a description of known behavior, or data to browse. The [.NET client's pattern API](/chronicle/clients/dotnet/patterns/) exposes these methods; the gRPC `Patterns` service provides the underlying queries.

| .NET method | Use it to |
| --- | --- |
| `GetUsualActions` | Find actions associated with a context, ranked by confidence |
| `GetPatterns` | Find patterns describing a supplied situation, ranked by specificity and then confidence |
| `GetPatternsForScope` | Browse all established patterns for one scope |
| `GetScopes` | List scopes with established patterns |

### Two questions, and why they are different calls

Use `GetUsualActions` when you know the situation but want to discover the action. Given Monday and early morning, it can return a pattern like `CommandType=RegisterInvoice; Day=Monday; TimeBucket=EarlyMorning`. It compares the pattern's context facets with your query and returns the command as part of the answer.

Use `GetPatterns` when you already know the action and want established patterns that describe it. Every facet in a returned pattern must be present with the same value in your query, including `CommandType` if the pattern names one. This can supply context for a review of an unfamiliar action, but an absent match is not an anomaly verdict.

`GetUsualActions` keeps at most one pattern per action. When several patterns name the same action, it keeps the one using the most context facets, then uses confidence and support to break ties. It ranks the resulting actions by confidence. `GetPatterns` instead ranks by specificity, meaning the number of facets in the pattern, and then confidence.

Both calls return an empty result when nothing matches above the requested confidence threshold. They do not substitute a lower-confidence pattern to fill the result set.

## Asking about a moment

When you have a scope and a point in time, the .NET and Elixir helpers below derive `Day` and `TimeBucket` and query usual actions for that context. Use them instead of duplicating the bucketing rule in your application: a different rule can put your query in a different slot from the recorded events.

| Client | Ask about a moment | Bucket a moment |
| --- | --- | --- |
| [.NET](/chronicle/clients/dotnet/patterns/) | `Patterns.GetPatternsAt(scope, moment?)` | `moment.ToTimeBucket()` |
| [Elixir](https://github.com/Cratis/Chronicle.Elixir/blob/main/Documentation/patterns.md) | `Chronicle.Patterns.get_patterns_at(scope, moment)` | `Chronicle.Patterns.time_bucket(moment)` |

The .NET helper defaults to now with the application's local offset; the Elixir helper defaults to now in UTC. For a question about a user's local time, pass a moment carrying the intended offset. Both helpers derive the day and time bucket from that moment without first converting it to UTC. Additional context constraints are supported, but the moment's derived `Day` and `TimeBucket` take precedence over conflicting supplied values. Check the selected client's documentation for its exact arguments rather than assuming other clients expose these helpers.

## Seeing patterns in the Workbench

Under an event store's namespace, use Behavior patterns to browse patterns across scopes. Its pivot view lets you filter and group by facets such as command, initiator type, day, or event source type. Scope is a filter within the view, so you can compare scopes side by side. Cards show confidence and occurrence counts; opening a pattern shows its facets, confidence, and support in more detail.

Use Pattern heatmap to inspect one scope's weekly routine. The grid places patterns by day and time bucket, shades each slot using its strongest pattern's occurrence count relative to the scope's busiest slot, and outlines the current slot. Clicking a cell lists its patterns. When the current slot contains a pattern, the panel above the grid shows its strongest pattern. This selection is based on occurrences, with confidence breaking ties; it is not the confidence-ranked result of [`GetUsualActions`](/chronicle/clients/dotnet/patterns/).

A pattern needs both `Day` and `TimeBucket` to appear in a grid cell. Patterns missing one or both dimensions remain available in the pivot view. An empty heatmap therefore does not necessarily mean the scope has no patterns.

## Trying it out

The Backoffice sample in the Chronicle repository generates a fictional half-year of accounts payable, ledger, procurement, HR, and payroll work. It includes agents acting for people, scheduled system work, and a person whose work has no regular day-and-time routine.

Follow the README in `Samples/Backoffice` to generate the history, then compare scopes in the Workbench. Look for different commands at different times of the week, and compare the heatmap with the pivot view for the person without a time-based routine. The generated history gives you a known scenario to compare with the mined summaries; it is not customer data.

To use the same queries in an application, continue with the [.NET pattern API](/chronicle/clients/dotnet/patterns/), which shows how to supply a scope and context.
