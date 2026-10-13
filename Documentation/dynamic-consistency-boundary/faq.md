---
title: Questions and answers
description: Short answers about making write-side rules hold with the Chronicle client, with links to the full explanation.
---

Use this page for a quick answer. Each answer links to the page that explains it.

## Does reading a read model before I append make my rule safe?

No. A read model shows state. It does not note where in the log it stopped, and it does not guard the append. Two requests can read the same state and both append. Use a [constraint](../constraints/index.md) for uniqueness and a [decision read](../read-models/decision-reads.mdx) for a rule over state. See [Choosing how a rule holds](./choosing.mdx).

## What is the difference between a plain read and a decision read?

A plain read is `IReadModels` or an injected read model. It can be materialized, which means stored and updated after the append returns, or passive, which means folded from the events when you read it. A decision read folds a fresh session, notes the log position before it folds, and gives you a guard for the append. Only a decision read protects the append.

## Is a passive read model safe for decisions?

A passive read is fresh at read time, but it notes no position and guards nothing. It is fine for a display or for a check you will repair. It is not a guard.

## Can I note the tail myself and then read my materialized read model?

No. A materialized read model can lag behind the log, so the state you see can be older than the tail you noted. The events between the two are neither seen nor guarded. A decision read avoids this by folding the events itself and checking that the fold reached the expected position.

## When do I use a constraint and when a decision read?

Use a constraint when the rule is that a value is unique or that something is created once. The kernel checks it inside the append, with no extra read, for every writer. Use a decision read when the rule depends on state you read, such as a count, a stock or a status. A constraint cannot count or compare.

## Can I enforce a rule that spans two entities?

Yes. Take one decision read per entity, and append one event per event source in a single `AppendMany` with `guardedBy` set to both reads. `AppendMany` is all-or-nothing. See [A rule across two keys](./scenarios.mdx#a-rule-across-two-keys).

## Why does a fact about two entities need two events?

An event has exactly one event source ID, and a decision read folds only the events of its own source. To let both entities' read models see the fact, write it once for each.

## Does an absent read model get guarded?

Yes. If the model does not exist when you read, a concurrent event that would create it makes your append conflict. That is how a decision read covers "create it only if it does not exist yet". For a pure create-once rule a constraint is simpler.

## Does Chronicle retry when the guard rejects my append?

No. A rejected append writes nothing and returns a result you must read. The new state can change the decision, so read again and decide again. A retry that reuses the old decision would defeat the guard.

## What does a decision read cost?

About four calls per attempt, a fold of the key's whole history of the model's event types every time, and up to three attempts. On a loaded machine a decision read took about 1.6 times a fresh passive fold. See [what a decision read costs](./choosing.mdx#what-a-decision-read-costs) for the numbers and what was not measured.

## Why was my decision read refused?

The model has a shape Chronicle cannot guard exactly: a reducer or self-reducing model, a join, children, a key that is not the event source ID, or no `Id` property. It throws `DecisionReadRefused` with a typed reason. Use a flat projection keyed by event source, with an `Id` property that is a string or a GUID. See [admitted projections](../read-models/decision-reads.mdx#admitted-projections).

## Why did my conflict not reach the HTTP response?

The ASP.NET Core middleware commits the unit of work after the action returns. By then the response is written. Commit yourself with `IUnitOfWorkCompletionFeature.CommitAsync()` before you respond. See [In an ASP.NET Core endpoint](./scenarios.mdx#in-an-aspnet-core-endpoint).

## Why did my guard not apply when I used the unit of work?

You probably called `eventLog.Append`. A plain `Append` is written at once and takes no part in the unit of work. Stage events with `eventLog.Transactional.Append`, or use `GetDetached` with `AppendMany(guardedBy:)`.

## Do tags take part in consistency?

No. A concurrency scope filters by event types and by event source ID, event source type, event stream type and event stream ID. Custom tags are stored and filterable but are not part of a scope. See [Dynamic Consistency Boundary in Chronicle](./chronicle.md#metadata-tags-and-custom-tags).

## How do I know a concurrency check actually ran?

Read `ConcurrencyCheckPerformed` on the append result. A skipped check and a passing one both succeed, and this property tells them apart. See [Concurrency](../events/concurrency.mdx#telling-a-skipped-check-from-a-passing-one).

## Does the kernel's own retry skip the guard?

No. When the kernel renumbers an append after a duplicate sequence number, it validates concurrency scopes again before it retries. It does not run constraints again on that path.

## What does a decision read not guard?

Revise and redact, a migration that replaces a generation, other event sequences, data outside the event log, foreign writers that do not follow the same rules, and a projection definition changed outside this client. See [Limits and assumptions](../read-models/decision-reads.mdx#limits-and-assumptions).

## What kernel version do I need?

Decision reads need kernel 19.0.0 or later. A stream-scoped read needs a kernel that acknowledges stream scope, and refuses with `StreamScopeNotSupported` otherwise.

## Do decision reads work from every client?

Decision reads are part of the .NET client. Constraints are enforced by the kernel, so they apply to appends from every client. Your SDK's own pages list what it offers.

## Why is protection opt-in?

Making it the default would change what existing code does, and a protected path refuses what it cannot guard exactly. See [Why protection is opt-in](./choosing.mdx#why-protection-is-opt-in).

## How do I test a rule?

Append a competing event between the read and the guarded append, and assert the conflict. A test without that interleaving cannot show the guard works. See [Test the race](./scenarios.mdx#test-the-race).

## Where do I go if I use Arc commands?

[Choosing how a command's rule holds](/arc/backend/csharp/chronicle/consistency/) and its [questions and answers](/arc/backend/csharp/chronicle/consistency/faq/).
