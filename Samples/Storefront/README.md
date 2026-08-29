# Storefront

An online retailer where different people do different jobs, for demonstrating **pattern detection** end to end.

Chronicle mines behavior patterns from the *context* events were appended in — who acted, on whose behalf, carrying
out which command, caused by what, against what kind of thing, and when. A store where everybody performs the same
workflow gives every person the same patterns, and there is nothing to discover in that. So this one has a
warehouse, a support desk, a buying office and a fraud review, and the people in them behave nothing like each
other.

## Running it

The sample talks to a Chronicle kernel on `localhost:35000` and uses an event store named `Storefront`.

| Key | Argument | What it does |
| --- | --- | --- |
| `G` | `generate` | Generate the backdated history |
| `S` | `scopes` | List the scopes that have established behavior |
| `P` | `patterns <scope>` | Show every pattern for a scope |
| `N` | `now <scope>` | Ask what usually happens right now |
| `Q` | | Quit |

Every command works as an argument too, so the sample can seed a demo environment from a script:

```shell
dotnet run --project Samples/Storefront -- generate
dotnet run --project Samples/Storefront -- patterns maya.chen
```

Run `generate` first. It appends **12,663 events** across half a year and takes a few minutes. It is
**re-runnable**: the first thing it appends is a marker guarded by a uniqueness constraint, so a second run is
turned away and leaves the store exactly as it found it.

It connects with TLS validation skipped, which a local kernel's development certificate needs. Set
`CHRONICLE_CONNECTION_STRING` to point it somewhere else.

> **Start the kernel *after* the store has event types registered, or restart it once after the first `generate`.**
> Pattern capture currently subscribes only at server startup, so against a brand new store it has nothing to
> subscribe to and mines nothing until the next restart — see
> [#3867](https://github.com/Cratis/Chronicle/issues/3867). Give the observer a minute to work through the history
> afterwards.

## Who does what

| Who | Their job | What gets discovered |
| --- | --- | --- |
| **Maya Chen** | Picks and packs, first thing every weekday | `PackShipment` · `EarlyMorning` — the busiest habit in the store |
| **Otto Brandt** | Sends the packed shipments out mid-morning | `DispatchShipment` · `Morning`, always `CausedByCommand=PackShipment` |
| **Lena Ferrari** | Works the support queue and the returns, afternoons | `AnswerTicket` · `Afternoon`, alongside a separate returns habit |
| **Ravi Kapoor** | Places the week's restock over lunch on a Monday | `RestockProduct` · `Monday` · `Midday` |
| **Nora Sandvik** | Reviews flagged orders in the evening | `HoldOrderForReview` · `Evening`, always caused by `PlaceOrder` |
| **Tobias Lund** | Covers whatever needs covering, whenever | Over a hundred weak patterns, and not one that pins down both a day and a time |

Nobody's patterns look like anybody else's, which is the point. Browsing them is worth doing because each scope
answers a different question, not the same question about a different person.

Three more things are in the data on purpose:

- **Agents acting for people.** A pricing agent moves prices overnight on Ravi's behalf, and a support assistant
  drafts replies on Lena's. Both file under the *person*, not the agent — Ravi's strongest pattern is
  `ChangePrice` · `Night` · `InitiatorType=Agent`, which is work he never does himself.
- **The system acting for nobody.** The overnight run tops stock up on a Sunday night, and carriers confirm
  deliveries in the evening. Both surface under a scope whose `InitiatorType` is `System`.
- **Causation chains.** Picking is caused by an order being placed, dispatch by packing, a return decision by the
  request, a review by the order. That is what gives the `CausedByCommand` facet real values to mine — without it
  the facet would be empty on every event in the store.

### About the control

Tobias is the counterweight. He ends up with *more* patterns than anyone — over a hundred — but each is weak, and
none of them pins down a day and a time together, because he never repeats a slot often enough for one to clear the
threshold. That contrast is the honest demonstration: the miner does not refuse to describe him, it describes him
accurately as somebody with no routine.

The heatmap makes the point without a word. Step through the scopes and you get three different pictures:

| Scope | The grid |
| --- | --- |
| **maya.chen** | One bright column — Early morning, Monday to Friday, and nothing else anywhere |
| **lena.ferrari** | Two columns — Afternoon, where she works, and Night, where her assistant works for her |
| **tobias.lund** | Empty. Not one slot established |

### About the numbers

`Occurrences` is a **bounded approximation**, not a tally. The miner keeps a fixed amount of memory per scope and
prunes combinations that stop being frequent, so a scope acting across many different facet combinations has its
counts under-reported more than a scope doing one narrow thing. Ravi's 546 is exact; Otto's dispatches report far
lower than the 962 in the log. Counts are comparable *within* a scope — which is what the heatmap shades by — but
are not a substitute for querying the event log.

## How the context gets set

Pattern detection reads none of the event *content*. An application built on **Arc** gets everything it reads for
free: the command pipeline names the command being executed, and the identity provider carries the acting user.
This sample appends through the Chronicle client directly, so it sets the same things by hand — see
`ActivityAppender`, which is the whole of it:

- the acting identity, with a delegation chain when an agent is acting for somebody;
- a causation naming the command, and the command that caused it;
- the event source type, which becomes the `AggregateType` facet;
- an explicit `occurred`, so the history is genuinely backdated.

That last one is not optional. Without it the server stamps append time, half a year of behavior collapses into the
few minutes the generator ran, and every pattern found is about the afternoon somebody ran this sample.

The generator also **plans the whole history first and appends it in the order things happened**. It works a habit
at a time, which would otherwise produce a log holding half a year of one person's mornings followed by half a year
of somebody else's. A real store's log is the day's work interleaved as people do it, and since the miner reads the
stream once in order, the order it arrives in is part of what it learns.

## What to open in the Workbench

Point the Workbench at the `Storefront` event store, then:

- **Behavior patterns** — the pivot over every scope at once. Filter by Scope to compare two people, or pivot by
  Command to see the whole vocabulary the store runs on. Pivot by Initiator to separate what people do from what
  their agents do for them.
- **Pattern heatmap** — the `Day × Time of day` grid for one scope. Step through Maya, Lena, Ravi and Tobias: the
  first three each light up somewhere different, and the last one does not light up at all.
- **Projections** — the query editor has real data to run against: `Order` and `Shipment` hold current state,
  `Product` accumulates stock from two different events, and `StaffActivity` counts what each person got through.
- **Event sequences** — the raw history, spread across the whole period rather than bunched at the end.

The `N` key in the console asks the same question the heatmap's panel asks, through `IPatterns.GetPatterns` — a
useful way to see that the Workbench view and the client API are the same answer.
