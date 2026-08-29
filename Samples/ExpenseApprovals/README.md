# Expense approvals

A store with recurring behavior deliberately baked into it, for demonstrating **pattern detection** end to end.

Chronicle mines behavior patterns from the *context* events were appended in — who acted, on whose behalf, carrying
out which command, caused by what, against what kind of thing, and when. This sample generates half a year of
expense-approval history in which some of that behavior is habitual and some of it deliberately is not, so the
patterns Chronicle establishes can be checked against what was actually put in.

## Running it

The sample talks to a Chronicle kernel on `localhost:35000` and uses an event store named `ExpenseApprovals`.

```shell
dotnet run --project Samples/ExpenseApprovals
```

| Key | What it does |
| --- | --- |
| `G` | Generate the backdated history |
| `S` | List the scopes that have established behavior |
| `P` | Show every pattern for a scope |
| `N` | Ask what usually happens right now |
| `Q` | Quit |

Press `G` first. It appends roughly 5,000 events across about 2,000 claims and takes a minute or so. It is
**re-runnable**: claim ids are derived from a fixed seed, and a uniqueness constraint on the submission rejects any
claim already in the store, so a second run skips what it already created rather than doubling the history.

Patterns do not appear the instant the events land — the pattern observer has to work through the history, and a
behavior only becomes a pattern once it clears the support and confidence thresholds.

## What is deliberately in the data

Five actors, four with a habit and one without:

| Who | Habit | What should be discovered |
| --- | --- | --- |
| **Dana Reeves** | Approves expenses Monday mornings | The strongest pattern in the set: `Day=Monday`, `TimeBucket=Morning`, `CommandType=ApproveExpenseReport` |
| **Nina Osei** | Approves over lunch, every weekday | A confident `TimeBucket=Midday` pattern with no single day attached to it |
| **Victor Hale** | Turns down travel claims late on a Friday | `Day=Friday`, `TimeBucket=Evening`, and `AggregateType=TravelClaim` — the one actor whose habit is about *what* is being decided as much as when |
| **Expense Assistant** | Approves small claims on Dana's behalf | Files under **Dana**, not under the agent, with `InitiatorType=Agent` |
| **Sam Doyle** | None — any day, any time, any decision | **Nothing.** Sam is the control |

Sam matters as much as the other four. A miner that reports patterns for Sam is finding structure in noise, and the
sample is as much a demonstration of that restraint as of the discovery.

Two further things are in the data on purpose:

- **The reimbursement run.** Approved claims are paid out by the system in the small hours of the following
  Tuesday. It surfaces as a pattern whose `InitiatorType` is `System` rather than a person or an agent.
- **Causation chains.** Every decision is appended as a command caused by the submission that led to it, and every
  reimbursement as one caused by the approval. That is what gives the `CausedByCommand` facet real values to mine —
  without it, the facet would be empty on every event in the store.

## How the context gets set

Pattern detection reads none of the event *content*. An application built on **Arc** gets everything it reads for
free: the command pipeline names the command being executed, and the identity provider carries the acting user. This
sample appends through the Chronicle client directly, so it sets the same things by hand — see `ActivityAppender`,
which is the whole of it:

- the acting identity, with a delegation chain when an agent is acting for somebody;
- a causation naming the command, and the command that caused it;
- the event source type, which becomes the `AggregateType` facet;
- an explicit `occurred`, so the history is genuinely backdated.

That last one is not optional. Without it the server stamps append time, months of behavior collapse into the few
minutes the generator ran, and every pattern found is about the afternoon somebody ran this sample.

## What to open in the Workbench

Point the Workbench at the `ExpenseApprovals` event store, then:

- **Behavior patterns** — the pivot viewer over everything established for a scope. Pick Dana's scope and pivot by
  `Day` or `Time of day`; switch to the Initiator dimension to separate her own approvals from the assistant's.
- **Pattern heatmap** — the `Day × Time of day` grid for one scope. Dana's Monday-morning cell is the darkest thing
  on the page. The panel above the grid answers "what does this scope usually do right now".
- **Projections** — the query editor has real, non-trivial data to run against: `ExpenseReport` holds the current
  state of every claim, and `SubmitterActivity` aggregates what each person has claimed.
- **Event sequences** — the raw history, backdated across the whole period rather than bunched at the end.

The `N` key in the console asks the same question the heatmap's panel asks, through `IPatterns.GetPatterns` — a
useful way to see that the Workbench view and the client API are the same answer.
