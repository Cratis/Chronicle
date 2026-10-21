<!-- cratis-ai-managed: skills/cratis-chronicle-event-modeling/references/event-log-hygiene.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Event log hygiene

An event log that fills with ticks, triggers and repeats makes every projection replay noise
forever, grows the database without the product doing more, and hides real changes among
restatements. The log holds domain facts only.

## Contents

- What is not an event
- Orchestration without events
- Zero repeats in observation commands
- Event names
- Counters need clean transitions
- Measure the log

## What is not an event

A fact is something that happened in the domain and that the domain would still care about
after the process that noticed it is gone. These are not facts:

- **Ticks and heartbeats**: "still alive", "checked again", "retried", anything recorded because
  time passed or a loop came round.
- **Signals**: an event appended so a reactor does something. `…Requested`, `…Started`,
  `…Skipped`, `…Deferred`, `…Undecided`, `…Concluded` and `…Passed` used as triggers describe work
  being asked for or the act of looking, not the world.
- **"When did we last look"**: a freshness timestamp belongs in a read model, a metric or job
  state. Do not derive it from the time of the last recorded event; that makes it mean "the last
  time anything was recorded".
- **Telemetry**: usage, token counts, cost, latency. Use metrics or an operational store unless
  the figure is itself a business fact (a billable amount).
- **Non-answers**: `Pending`, `Computing`, "could not decide", "nothing was judged". The previous
  fact stays; the process logs the non-answer and a later attempt answers.
- **Retry policy**: how often something was tried and when to try again lives in the retrying
  process.

## Orchestration without events

Pick the mechanism that owns the progress, so progress is state of that mechanism, not events:

- a **durable job** (Cratis.Orleans jobs, see **cratis-orleans-jobs**) for work with steps, retries and pacing;
- an **event-sourced state machine** when the lifecycle is itself a domain fact sequence;
- an **Orleans reminder** to wake up later;
- a **command operation** (**cratis-arc-command-operation**) for inline work with compensation;
- **`ICommandPipeline`** (**cratis-arc-command-execution**) when a reactor or job must run a command.

## Zero repeats in observation commands

An *observation command* takes input from outside the model's own decisions: a webhook, a poll,
a sweep, a retry, a classifier result. The same fact is never recorded twice in a row on one stream.

- `Handle()` takes the thing it would change and returns no events when they agree. A bare
  `=> new SomethingChanged(...)` is a defect for any command fed by polling, webhooks, retries or
  classifiers.
- Compare **each fact on its own**, so a change in one does not re-record the others.
- Compare against something that **cannot lag**: a `[Passive]` read model read by key, or the
  stream's own events. A materialized read model trails the log, so a guard reading it is bypassed
  exactly while it is rebuilding or behind. A stale comparison may cost one extra event; it must
  never drop a change. A poller may instead compare with the last-look store it writes itself.
- A passive model proves existence only through a flag set by the first event; read
  [vertical-slices.md](../../../rules/vertical-slices.md) "Existence checks" first.
- Specify two behaviors per observation command: the **same observation twice appends nothing**,
  and a **changed observation appends exactly the changed fact**. A command that can judge
  nothing also gets a specification that "nothing judged" appends nothing.

## Event names

`…Changed` is a promise that something changed. An event of that name that can be appended with
nothing changed is wrong; so is `…Observed` appended on every sighting. Split a broad
`…DetailsChanged` into one event per fact (`…BodyChanged`, `…LabelsChanged`) so a change in one
field records one event and consumers subscribe to the facts they care about.

## Counters need clean transitions

`[Increment]`/`[Decrement]` projections know the event, not the item's previous state. A close for
an already closed item, or an add for one already present, drifts the counter permanently, and
replay reproduces the drift (a "failing builds" counter went deeply negative this way). The command that
appends a lifecycle event refuses a non-transition and appends nothing. Before shipping a counter,
fold the log per event source with the proposed mapping and compare the result with the state it
summarizes; any difference means the events are not clean transitions.

## Measure the log

- Set an **event volume budget** from the product's real activity (events per entity lifecycle and
  per day) and alarm on a sustained rate far above it. A rate above budget is a defect to find.
- Count events per type and per day, then find `(event type, event source)` pairs with **identical
  consecutive events** (same payload, ignoring sequence number and occurred time). Those are repeats a
  missing comparison produced; each pair names the command to fix.
- Per entity, compare the measured events per lifecycle with the modeled ones; the difference is
  almost always non-facts.
