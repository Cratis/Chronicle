# Behavior Patterns

Chronicle mines the event history of a store for **recurring behavior** — combinations of context that keep leading to the same action — and lets you ask what usually happens in a situation. The answer is backed by what actually happened, not by a guess.

The question it answers is: *given this user, this day, this time of day, what does this person normally do?* An agent, a workflow, or the Workbench can ask it and act on a real answer, or learn that there is no established behavior and say so.

## What a pattern is

A **behavior pattern** is a combination of facets that recurred often enough and reliably enough to survive:

| Part | Meaning |
| --- | --- |
| **Grouping key** | The scope the behavior belongs to — normally the user |
| **Facets** | The contextual dimensions the pattern constrains, and their values |
| **Occurrences** | How many times it has been observed |
| **Confidence** | How often it holds when its context is present, 0 to 1 |
| **Support** | The share of all observed events it was seen in, 0 to 1 |
| **Weight** | Recency-weighted strength — decays as the behavior goes unseen |
| **First seen / last seen** | When it was first and last observed |

A pattern such as `{ Day: Monday, TimeBucket: EarlyMorning, CommandType: RegisterInvoice }` with a confidence of `0.9` reads as: *first thing on a Monday, this person enters supplier invoices nine times out of ten.*

## Facets

Facets are read off an event's **context** — never its content — so the same vocabulary applies to every event type in every event store.

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

Every time-derived facet comes from the event's own **occurred** timestamp, never from wall-clock time at processing. A backdated append and a replay both land in the bucket the event actually belongs to.

### Who the behavior belongs to

An agent acting on behalf of a person contributes to **that person's** behavior, not its own — otherwise one habit would be split across every agent that happened to carry it out. The agent is still recorded as the initiator, so agent-driven and user-driven behavior stay distinguishable.

An event nobody can be named for is not mined at all. Its behavior belongs to no scope, and counting it into a catch-all that every unattributed append pours into produces noise rather than a pattern.

## How mining works

Chronicle does not store anything per event. It keeps a bounded **Lossy Counting** sketch per scope:

1. Each event's facets are expanded into every combination up to `MaximumCombinationSize` (3 by default). The cap is what keeps the candidate space polynomial rather than exponential.
2. Each combination is counted in the sketch. Memory is bounded by the `Error` parameter regardless of how long the stream runs — nothing with a true frequency above the support threshold is ever missed.
3. A combination that goes unseen **decays**: its weight is multiplied by `DecayFactor` for every day since it was last seen, so behavior that stopped happening sinks below the threshold and is pruned instead of competing forever with what a person does now.
4. Only combinations clearing both `MinimumSupport` and `MinimumConfidence` are persisted.

Storage therefore scales with **distinct recurring behavior**, not with event volume. A store that appends millions of events but sees a few hundred recurring behaviors holds a few hundred rows.

Mining happens in memory on every observed event; **persistence is deferred** — the scopes an interval touched are rewritten on the `PersistenceInterval` cadence, so a bulk ingest of thousands of events costs a handful of writes rather than one rewrite per event. Sketches are kept **per event store and per namespace**: the same scope name in two stores — or two tenants' namespaces — is two different people's behavior and never counts into one sketch.

### Across restarts

The sketch lives in memory, but what survived it is persisted — so when the server restarts, a scope's established patterns are **restored into the sketch before anything new is mined for it** and counting continues where it left off. Without that, the first events after a restart would be a fresh sketch's only observations, held with full support, and rewriting the scope from them would wipe established behavior. `Occurrences` stays a bounded approximation: a small tail of events can be re-mined after a crash, over-counting by at most the observer's checkpoint window.

### Confidence

Confidence reads as the rule *"in this context, this action follows"*: the frequency of the whole combination over the frequency of the same combination without its `CommandType` facet. A combination that names no action is pure context, and its confidence is its support.

## Configuration

Under `Cratis:Chronicle:PatternDetection`:

| Setting | Default | What it does |
| --- | --- | --- |
| `Facets` | `CommandType`, `InitiatorType`, `CausedByCommand`, `AggregateType`, `Day`, `TimeBucket` | Which facets take part in the mined combination |
| `MaximumCombinationSize` | `3` | The largest number of facets a combination may hold |
| `Error` | `0.001` | The Lossy Counting error parameter — smaller buys accuracy with memory |
| `MinimumSupport` | `0.01` | The smallest share of events a combination must hold to survive |
| `MinimumConfidence` | `0.5` | The smallest confidence a combination must hold to survive |
| `DecayFactor` | `0.99` | Daily decay applied to a combination that has gone unseen |
| `PersistenceInterval` | `5` | Seconds between persisting the scopes mining has touched |

`Year` and `Month` are deliberately absent from `Facets`: they are kept on a surviving pattern for recency, but combining them multiplies the candidate space by every month the store has been running while splitting one behavior across all of them. Add them when a deployment wants to mine seasonality and can afford the cardinality.

## When capture starts

Pattern capture observes the event types an event store has registered, and re-subscribes as more are registered — so a store that gains its first event types while the server is already running starts being captured then, not at the next restart. A **namespace added while the server runs** is subscribed the moment it appears, so a tenant onboarded between restarts is mined from its first event.

## Naming the command

For `CommandType` and `CausedByCommand` to be meaningful, something above the event has to name the command. [Cratis Arc](/arc/) records a `Command` causation naming the executing command for the duration of that command, so nested commands produce a real "caused by" chain and sibling commands do not. Anything else appending to Chronicle can do the same by putting a `commandType` property on its causation.

Without such a link the mined `CommandType` falls back to the event type, which is still a meaningful action in an event-sourced store — the fact that was recorded *is* what happened.

## Querying patterns

Ask what usually happens with the [.NET client's pattern API](/chronicle/clients/dotnet/patterns/), or over gRPC through the `Patterns` service:

- **`GetUsualActions`** — given a situation, what is usually *done* in it, ranked by confidence.
- **`GetPatterns`** — given a situation, the patterns that *describe* it, ranked by specificity and then confidence.
- **`GetPatternsForScope`** — everything a scope has established, unfiltered, for browsing.
- **`GetScopes`** — the scopes that hold any patterns at all, which is what a browsing view needs before it can offer one.

### Two questions, and why they are different calls

Most applications want the first one: *what does this person usually do here?* You describe the situation you are in — a day, a part of the day, the kind of thing being worked on — and the answer is the command.

The second is the opposite question: *is what I am looking at normal?* There you already know the command, and you want the established patterns that cover it — which is what you check an action against before flagging it as unusual.

They cannot be one call, because they compare different halves of a pattern. A pattern like `CommandType=RegisterInvoice; Day=Monday; TimeBucket=EarlyMorning` **describes** a situation only if everything it names was named in the question. But nobody asking what usually happens on a Monday morning can name the command — naming it is what they are asking for. So `GetUsualActions` compares only the *context* half of a pattern against your question and hands back the command as the answer, while `GetPatterns` compares the whole pattern and can only ever return facets you already named.

`GetUsualActions` returns **at most one answer per action**. A habit is established at several context sizes at once — on a Monday, in the early morning, and on a Monday early morning are three mined combinations describing one behavior — and returning all three says the same thing three times while pushing the second-most-likely action out of a limited result set. The one kept is the one conditioned on most of your question.

Ranking differs for the same reason. `GetPatterns` leads with specificity, because the pattern covering most of what you asked describes your situation best. `GetUsualActions` leads with **confidence**, which is already the chance of that action given the context it was established in — a number that compares directly between one answer and the next, where a facet count does not.

**Nothing clearing the confidence bar returns nothing.** An empty answer is a true statement — this scope has no established behavior for this context — and is not padded with the best of a bad set.

## Asking about a moment

The context an application usually has is a person and a point in time, so every client offers that as a single call on top of `GetUsualActions`: **give it the scope, optionally give it a moment, and it fills in the rest**. The moment defaults to now.

What it fills in is the `Day` and the `TimeBucket`, derived from the moment with **the same rule the engine used when it mined the events**. That rule is the load-bearing part. A caller deriving the bucket itself owns a second copy of it, and when the two drift nothing fails loudly — the query simply asks about a slot the mining never used and comes back empty. Each client therefore exposes the bucketing rule as well, so the copy never has to exist:

| Client | Ask about a moment | Bucket a moment |
| --- | --- | --- |
| [.NET](/chronicle/clients/dotnet/patterns/) | `Patterns.GetPatternsAt(scope, moment?)` | `moment.ToTimeBucket()` |

Other clients — TypeScript, Kotlin, Elixir, Python — expose the same capability in their own idiom; see the client's own page for the exact shape. The contract they share is the one above: scope required, moment optional and defaulting to now, day and time bucket derived by the engine's rule rather than the caller's.

## Seeing patterns in the Workbench

Two views under an event store's namespace read the same patterns from different angles.

**Behavior patterns** pivots every pattern in the namespace, with the scope as the first filter rather than a control above the viewer — so two people's behavior can be compared side by side instead of one being chosen before anything can be seen. The facets become the dimensions and filters, so the same set can be approached from whichever direction the question comes: by command, by who initiated it, by day or part of day, by aggregate, by what caused it, or by how specific the pattern is. Each card carries a confidence bar, so a well-established habit is distinguishable from a marginal one without reading numbers.

**Pattern heatmap** answers the narrower question of *when*, for one scope at a time. It is a day-by-time-of-day grid where each cell is shaded by how much the scope does in that slot, with the current slot outlined. Activity rather than confidence, because confidence saturates — anything habitual sits at a hundred percent, so shading by it would make every slot a person works in look the same. Clicking a cell lists everything established for it. Above the grid, a panel names what the scope usually does *right now* — the same question [`GetUsualActions`](/chronicle/clients/dotnet/patterns/) answers from code, asked with the current day and time bucket as the context.

A pattern that constrains neither day nor time of day belongs to no cell and is left out of the grid rather than drawn somewhere arbitrary. It is still in the pivot view, which is the one that shows everything.

## Trying it out

The **Backoffice** sample in the Chronicle repository generates half a year of work in the internal system a mid-size company runs on — accounts payable, the ledger, procurement, HR and payroll — plus agents acting on people's behalf, an overnight system run, and one person with no routine at all as a control.

It is the quickest way to see what the views are for, because nobody in it does only one thing. Everybody holds three or four different jobs at different points in the week, so each person's heatmap is a different shape with several commands in it rather than one dominant block: the accounts payable clerk lights up first thing and again at lunchtime with different work in each, the controller lights up through the afternoons and twice a week at midday, and the person who covers whatever is short-handed lights up nowhere. Its README explains what was put in, so what Chronicle establishes can be checked against it.
