<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/performer-bodies.md -->
# Performer bodies are rendered

Reach for a performer body when the declarative query cannot say what the read needs, for
example a filter relative to today: clock operands are syntax-only in query filters, so the
body computes it. Attach the body with `file` (or an inline `csharp` fence):

```screenplay excerpt
query FiguresInRegion => InvoiceFigures[]
  filter region String
  filter status String optional
  performer
    file Queries/FiguresInRegion.cs
```

## Snapshot contract

The body returns the read-model instance (or null) for a single query, or
`IEnumerable<ReadModel>` for a collection. Its `readModels` is the current collection of only
the declared read model. It sees caller arguments by camelCase name and on `context.Arguments`,
and `context.Tenant`, `context.Identity` and `context.ReceivedAt`, for example
`return readModels.Where(figures => figures.Total > 0);`.
Screenplay's Snapshot Many provider seam `PerformAsync` is not invoked by reference execution:
performer queries remain outside its executable model, not empty successful results.

## Live collection contract

Requires Screenplay 4.256.0 for the live collection typed contract.

An `observable ReadModel[]` performer has `ContextVersion` 1 and `ResultVersion` 2.
Its host-defined `LiveReadModels<T>` exposes only `IObservable<IEnumerable<T>>`, where T is the
declared read model: the initial/current collection and later emissions are complete snapshots, not deltas.
It does not permit reads of other models or guarantee that changes occur. The body returns
`IObservable<IEnumerable<ReadModel>>`. An observable single or optional result has no live contract;
do not extrapolate this collection shape to either.

Use one subscription per client; dispose on unsubscribe, disconnect or shutdown. `OnError` and
`OnCompleted` terminate it; the contract does not resubscribe. Arguments and context are captured
once per invocation, `ReceivedAt` stays fixed, and context has no cancellation member.
The host owns subscription lifetime and resource limits, not a fictional context token.

## Stage target gate

Stage renders the read model record and the query method with its route, parameters (filters in
declaration order, optional ones defaulted) and the modeled `[Authorize]` policy, and embeds the
verified body in a compile-checked helper; no projection or reducer is needed. Do not hand-write
a supported performer as gap-fill: attach the body to the model and render. An observable
performer, a non-C# body, a missing, changed or hash-invalid body, or a body that does not compile
becomes an explicit implementation gap instead, with a not-fully-runnable marker (see
[bodies and implementation gaps](../../cratis-stage-rendering-and-sandbox/references/current-boundaries.md#bodies-and-implementation-gaps)).

Requires Stage 4.197.0 for supported snapshot performers (`cratis --version`; CLI 3.100.0 bundles Stage 4.196.0).
Older versions keep gap-fill.
Screenplay's live type/contract binding is not evidence that this Stage baseline executes live bodies.

Source: Screenplay v6.32.0 `Documentation/screenplay/queries.md`, `context.md` and
`ISemanticBodyExecutionProvider`; Stage renderer behavior is a separate target contract.
