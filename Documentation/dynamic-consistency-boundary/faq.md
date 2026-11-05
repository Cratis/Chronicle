---
title: 'Chronicle consistency: questions and answers'
description: Chronicle constraints enforce uniqueness; decision reads guard state-dependent writes. Find answers about races, conflicts, costs and read protection.
---

Chronicle uses constraints to enforce uniqueness and decision reads to protect writes that depend on state. A plain read model does not guard a later append. These answers explain which mechanism to choose and what to check when a guard is missing or an append conflicts.

## Is Chronicle a DCB event store?

Chronicle supports Dynamic Consistency Boundary (DCB)-style decision models and conditional appends, but does not implement the DCB specification's API or tag-based consistency model. Each event has one event source ID. Custom tags can be stored and filtered, but cannot define an append condition. See [Dynamic Consistency Boundary in Chronicle](./chronicle.md) for the comparison and [DCB examples in Chronicle](./dcb-examples.mdx) for the alternatives.

## Does reading a read model before I append make my rule safe?

No. A read model shows state. It does not note where in the log it stopped, and it does not guard the append. Two requests can read the same state and both append. Use a [constraint](../constraints/index.md) for uniqueness and a [decision read](../read-models/decision-reads.mdx) for a rule over state. See [Choosing how a rule holds](./choosing.mdx).

## What is the difference between a plain read and a decision read?

In .NET, a plain read goes through `IReadModels` or an unguarded injected read model. It can be materialized, with stored state updated asynchronously, or passive, with state folded from events at read time. Neither supplies an append guard. A decision read captures the log position before folding a fresh session and returns a guard with the state. Pass that guard to the append, or enroll the read in the unit of work that commits the events.

## Is a passive read model safe for decisions?

A passive read is fresh at read time, but it notes no position and guards nothing. It is fine for a display or for a check you will repair. It is not a guard.

## Can I note the tail myself and then read my materialized read model?

No. A materialized read model can lag behind the log, so the state you see can be older than the tail you noted. The events between the two are neither seen nor guarded. A decision read avoids this by folding the events itself and checking that the fold reached the expected position.

## When do I use a constraint and when a decision read?

Use a constraint when the rule is that a value is unique or that something is created once. The kernel checks it inside the append, with no extra read, for every writer. Use a decision read when the rule depends on state you read, such as a count, a stock or a status. A constraint cannot count or compare.

## Can I enforce a rule that spans two entities?

Yes. Take one decision read per entity and pass both to `AppendMany` through `guardedBy`. Append an event to each source whose history must change. The batch is all-or-nothing, and a source you only read can be guarded without receiving an event. See [A rule across two keys](./scenarios.mdx#a-rule-across-two-keys).

## Why does a fact about two entities need two events?

It needs two events when both histories must record the change. Each event has one event source ID, and each decision read folds its own source's events. A course subscription therefore records a seat taken on the course and a course joined on the student. Reading another entity does not itself require writing to it: a product-price check can guard product reads while writing only the order. See [When one fact becomes two events](./chronicle.md#when-one-fact-becomes-two-events).

## Where do event source definitions fit?

A definition names the event source type, the streams and the concurrency dimensions an append is guarded by. A decision read can be scoped to one stream, and appending through the definition makes the event carry the same values as the guard. See [Event sources and the Dynamic Consistency Boundary](../event-sources/dcb.mdx).

## Does an absent read model get guarded?

Yes. If the model does not exist when you read, a concurrent event that would create it makes your append conflict. That is how a decision read covers "create it only if it does not exist yet". For a pure create-once rule a constraint is simpler.

## Does Chronicle retry when the guard rejects my append?

No. A rejected append writes nothing and returns a result you must read. The new state can change the decision, so read again and decide again. A retry that reuses the old decision would defeat the guard.

## What does a decision read cost?

Each attempt captures tail positions and folds the key's history of the model's event types in a fresh session; it does not use the stored read model. The .NET client makes up to three attempts if the fold has not reached the expected events or has run beyond the captured boundary. Longer histories increase the work, and more concurrent matching writes increase conflicts. See [what a decision read costs](./limits.mdx#what-a-decision-read-costs), then measure with your own model and write rate.

## Why was my decision read refused?

Check the typed reason on `DecisionReadRefused`. For a .NET projection, start with a flat model keyed by event source and an `Id` property that is a string or GUID. Joins, child collections and keys derived from other values are refused because the client cannot derive an exact guard for them.

Classic reducers cannot have filters or constructor dependencies other than `ILogger<T>`. Self-reducing read models are exempt from that dependency restriction, but not from the filter restriction. See [admitted projections](../read-models/decision-reads.mdx#admitted-projections) and [reducers](../read-models/decision-reads.mdx#reducers) for the full admission rules.

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

Other event sequences, data outside the event log, a migration that replaces a generation, and a projection definition changed outside this client are not covered by the append filter. A position alone also misses revisions and redactions; the .NET decision-read guard carries a mutation epoch when the kernel supplies one, allowing the kernel to reject a decision after history changes. See [history mutation](./chronicle.md#history-mutation-is-outside-the-guard) for that distinction.

A guard detects matching events whoever appended them, but protects only the decision that carries it. Another writer deciding without a guard can still break the rule. See [Costs and limits](./limits.mdx#what-is-outside-the-guard) and [Limits and assumptions](../read-models/decision-reads.mdx#limits-and-assumptions).

## What kernel version do I need?

The .NET client needs a kernel that reports the last handled event position for a read-model session. A stream-scoped read also needs the kernel to acknowledge that scope; otherwise the client refuses it with `StreamScopeNotSupported`. Keep the client and kernel versions aligned, and check refusal reasons rather than assuming a read was guarded.

## Do decision reads work from every client?

The .NET and Elixir clients expose decision reads, but their APIs and supported models differ. Do not assume that every client has the same read-protection integration. Constraints are enforced by the kernel and apply to appends from every client. Check your SDK's own pages before choosing an API.

## Why is protection opt-in?

Making it the default would change what existing code does, and a protected path refuses what it cannot guard exactly. See [Why protection is opt-in](./choosing.mdx#why-protection-is-opt-in).

## How do I test a rule?

Append a competing event between the read and the guarded append, and assert the conflict. A test without that interleaving cannot show the guard works. See [Test the race](./scenarios.mdx#test-the-race).

## What is read protection?

Read protection lets the code that owns a unit of work enforce guarded reads throughout an invocation. `IDecisionReads.Get` and `GetFor` enroll reads for its commit. Unguarded plain reads, detached reads and immediate appends are refused rather than silently bypassing the decision's guard. Explicit allowances let you identify reads that are advisory or contribute their own guard. See [Read protection](../read-models/read-protection.mdx).

## What is the difference between an advisory and a contributed read allowance?

Both open a region where a raw read is allowed. An `Advisory` read guards nothing: use it for a screen, a hint or a log, where a stale value is fine. A `Contributed` read influences the decision, so you enroll it yourself with `EnrolContributedRead` before you use the value, and it becomes part of the guard. Both need a reason. See [Read allowances](../read-models/read-protection.mdx#read-allowances).

## When should I enforce read protection?

When you own the code that begins and completes the unit of work, such as a command pipeline or a request host, and the rules there depend on state. Skip it for a single guarded append, where passing the decision read to `AppendMany(guardedBy:)` is enough, and for uniqueness, where a constraint is simpler. Protection refuses what it cannot guard exactly and costs a fold per read, so enable it where the rules need it.

## Is read protection the same as Arc's read-protection setting?

They are two layers. Read protection is the Chronicle API: the frame, the allowances and the refusals. Arc's setting is how an Arc project drives that API for its commands. [How Arc uses this API](../read-models/read-protection.mdx#how-arc-uses-this-api) maps one to the other.

## Why did my guard cover more than I read?

Guards only widen when they merge: reads of one source in different streams, a declared dimension set on a definition, or a read-protection frame each add to it. The result can cause extra conflicts but never misses one. See [How guards are derived](./how-guards-are-derived.mdx).

## How do I see what an append is guarded by?

Under a read-protection frame, call `IUnitOfWork.GetAppendConditions()` after the commit. It lists the effective conditions with their event types, stream values, positions and origins. More than one condition can refer to the same source. See [See what a commit guarded](./how-guards-are-derived.mdx#see-what-a-commit-guarded).

## Can I read two streams of one source in the same decision?

Yes, but the merged guard widens to what the reads have in common, so a change in either stream can conflict. If the rule truly spans two streams, read the whole source once so the guard says what it covers. If it does not, split it into two decisions. The merge rules are in [Several reads on one source](./how-guards-are-derived.mdx#several-reads-on-one-source).

## Do I need read protection to use decision reads?

No. A decision read works on its own: pass it to `AppendMany(guardedBy:)` or take it through the unit of work. Read protection is an option for code that owns the unit of work. It refuses a plain read or an immediate append in the same invocation, so a decision cannot rest on an unguarded read by mistake. See [Read protection](../read-models/read-protection.mdx).

## When should I use a per-stream read model?

When a rule is about one stream of an entity, such as one account in one month, and you want every read of the model to fold only that stream. Declare `[EventStream<TSource>("Transactions")]` on the model and pass the stream ID on each read. Use a whole-source decision read when the rule spans the entity. See [Read models scoped to one event stream](../read-models/per-stream.mdx) and the [scenario](./scenarios.mdx#a-limit-inside-one-stream-with-a-per-stream-read-model).

## What does Arc's read protection On add?

Arc's read protection has three modes: Off, Measure and On, with Off as the default. On manages guarded reads and the owner commit around command execution. Read-model parameter binding can supply decision reads, while explicit allowances distinguish advisory reads from reads that contribute another guard. Unguarded plain reads and immediate appends are refused inside the protected invocation.

Arc drives the Chronicle API described in [Read protection](../read-models/read-protection.mdx#how-arc-uses-this-api). For configuration, parameter binding and Measure mode, use [Choosing how a command's rule holds](https://www.cratis.io/arc/backend/csharp/chronicle/consistency/).

## Where do I go if I use Arc commands?

Start with [Choosing how a command's rule holds](/arc/backend/csharp/chronicle/consistency/) to choose the protection Arc applies around command execution. Its [questions and answers](/arc/backend/csharp/chronicle/consistency/faq/) cover integration-specific behavior.
