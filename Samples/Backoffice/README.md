# Backoffice

The internal system a mid-size company runs on — accounts payable, the ledger, procurement, HR and payroll — for
demonstrating **pattern detection** end to end.

Chronicle mines behavior patterns from the *context* events were appended in: who acted, on whose behalf, carrying
out which command, caused by what, against what kind of thing, and when. What makes that worth browsing is people
whose weeks look nothing like each other — so the thing this sample gets right is that **nobody here does only one
thing**. Everybody holds three or four different jobs at different points in the week, plus the ordinary employee
work everybody does.

## Running it

The sample talks to a Chronicle kernel on `localhost:35000` and uses an event store named `Backoffice`.

| Key | Argument | What it does |
| --- | --- | --- |
| `G` | `generate` | Generate the backdated history |
| `S` | `scopes` | List the scopes that have established behavior |
| `P` | `patterns <scope>` | Show every pattern for a scope |
| `N` | `now <scope>` | Ask what usually happens right now |
| `Q` | | Quit |

Every command works as an argument too, so the sample can seed a demo environment from a script:

```shell
dotnet run --project Samples/Backoffice -- generate
dotnet run --project Samples/Backoffice -- patterns ingrid.holm
```

Run `generate` first. It appends **11,337 events** across half a year and takes a few minutes. It is
**re-runnable**: the first thing it appends is a marker guarded by a uniqueness constraint, so a second run is
turned away and leaves the store exactly as it found it. Give the pattern observer a minute afterwards to work
through the history.

It connects with TLS validation skipped, which a local kernel's development certificate needs. Set
`CHRONICLE_CONNECTION_STRING` to point it somewhere else — useful if something else already holds port 35000.

## The week

| Who | What they do | When |
| --- | --- | --- |
| **Ingrid Holm** — accounts payable | Enters the post, matches invoices to orders, chases the ones that do not add up | First thing · lunchtime · Thursday afternoons |
| **Petter Aas** — controller | Posts to the ledger, releases payments, closes the period | Afternoons · Tue & Fri lunchtimes · Friday evenings |
| **Rania Haddad** — HR | Decides leave, reads applications, answers the Monday payroll questions | First thing · Tue & Wed afternoons · Monday mornings |
| **Jonas Vik** — payroll | Checks hours, runs the pay, fields the questions that follow | Monday mornings · Thursday evenings · afternoons |
| **Mira Sandhu** — procurement | Raises orders, accepts quotes, reviews suppliers | Lunchtimes · Wednesday mornings · Friday afternoons |
| **Alex Berg** — office manager | Covers whatever is short-handed | Whenever |

## What the heatmap shows

This is what the grids actually look like — the same query the Workbench heatmap runs, one cell per slot, labelled
with the command that dominates it:

```
=== ingrid.holm ===                                  === mira.sandhu ===
           EarlyM  Morning  Midday   Aft'noon  Night            EarlyM  Morning   Midday    Aft'noon
Monday     Registe    .     MatchInv    .      Registe  Monday     .        .      RaisePurc    .
Tuesday    Registe    .     MatchInv    .      Registe  Tuesday    .        .      RaisePurc    .
Wednesday  Registe    .     MatchInv    .      Registe  Wednesday  .     ApproveQu RaisePurc    .
Thursday   Registe    .     MatchInv DisputeIn Registe  Thursday   .        .      RaisePurc    .
Friday     Registe    .     MatchInv    .      Registe  Friday     .        .      RaisePurc ReviewSup
```

Nobody's grid looks like anybody else's, and no single operation dominates any of them. Ingrid's shows four
different commands; Rania's shows five. Step through the scopes in the Workbench and each one is a different shape.

**Alex's grid is empty.** He covers whatever is short-handed at no particular time, so he never repeats a slot often
enough for one to clear the threshold. He ends up with plenty of patterns — just none that pin down a day and a
time together. That contrast is the honest half of the demonstration: the miner describes him accurately as
somebody with no routine rather than inventing one.

## What else is in the data on purpose

- **Agents acting for people.** An invoice-capture agent works the mailbox overnight on Ingrid's behalf, and a
  timesheet checker does the same for Jonas. Both file under the *person*, which is why Ingrid's Night column is lit
  with work she never does herself — visible in the pivot by filtering on `InitiatorType`.
- **The system acting for nobody.** The overnight run reconciles the ledger on weeknights and files the closed
  period away on a Sunday. It surfaces under a scope whose `InitiatorType` is `System`.
- **Causation across colleagues.** An invoice is matched against an order *procurement* raised; a payment is
  released against an invoice *accounts payable* matched; a timesheet is checked because somebody handed it in.
  Every one of those is a real `CausedByCommand` value, and the chain crosses between people the way back-office
  work actually does.
- **Seven kinds of thing** — invoices, purchase orders, suppliers, the ledger, leave requests, candidates and
  timesheets — so `AggregateType` genuinely separates "Ingrid at midday on an invoice" from anything else.

### About the numbers

`Occurrences` is a **bounded approximation**, not a tally. The miner keeps a fixed amount of memory per scope and
prunes combinations that stop being frequent, so a scope acting across many facet combinations has its counts
under-reported more than one doing something narrow. Counts are comparable *within* a scope — which is what the
heatmap shades by — but are not a substitute for querying the event log.

## How the context gets set

Pattern detection reads none of the event *content*. An application built on **Arc** gets everything it reads for
free: the command pipeline names the command being executed, and the identity provider carries the acting user.
This sample appends through the Chronicle client directly, so it sets the same things by hand — see
`ActivityAppender`:

- the acting identity, with a delegation chain when an agent is acting for somebody;
- a causation naming the command, and the command that caused it;
- the event source type, which becomes the `AggregateType` facet;
- an explicit `occurred`, so the history is genuinely backdated.

Two things about *when* matter as much as the events themselves. The generator **plans the whole history first and
appends it in the order things happened** — a real log is the week's work interleaved as people do it, and the miner
reads the stream once in order. And where a chain needs a colleague to have acted first, that earlier event is
written **at the colleague's own time of day**, not offset from whoever is acting now, so one person's routine does
not smear across everybody who depends on it.

## What to open in the Workbench

Point the Workbench at the `Backoffice` event store, then:

- **Behavior patterns** — the pivot over every scope at once. Filter by Scope to compare two people side by side,
  pivot by Command to see the whole vocabulary the office runs on, or pivot by Initiator to separate what people do
  from what their agents and the overnight run do.
- **Pattern heatmap** — step through Ingrid, Petter, Rania, Mira and Alex. Five people, five different pictures,
  and one blank one.
- **Projections** — `Invoice` and `LeaveRequest` hold current state, `Ledger` accumulates a month of postings, and
  `StaffActivity` counts what each person got through. Its empty columns say the same thing the patterns do.
- **Event sequences** — the raw history, spread across the whole period rather than bunched at the end.

The `N` key in the console asks the same question the heatmap's panel asks, through `IPatterns.GetPatterns` — a
useful way to see that the Workbench view and the client API are the same answer.
