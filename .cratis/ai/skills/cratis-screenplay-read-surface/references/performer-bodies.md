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

The body is synchronous C# that returns the read-model instance (or null) for a single query,
or `IEnumerable<ReadModel>` for a collection. It sees `readModels` (the current collection), the
caller arguments by camelCase name and on `context.Arguments`, and `context.Tenant`,
`context.Identity` and `context.ReceivedAt`, for example
`return readModels.Where(figures => figures.Total > 0);`.

Stage renders the read model record and the query method with its route, parameters (filters in
declaration order, optional ones defaulted) and the modeled `[Authorize]` policy, and embeds the
verified body in a compile-checked helper; no projection or reducer is needed. Do not hand-write
a supported performer as gap-fill: attach the body to the model and render. An observable
performer, a non-C# body, a missing, changed or hash-invalid body, or a body that does not compile
becomes an explicit implementation gap instead, with a not-fully-runnable marker (see
[bodies and implementation gaps](../../cratis-stage-rendering-and-sandbox/references/current-boundaries.md#bodies-and-implementation-gaps)).

Requires Stage 4.197.0 to render performer bodies, run directly or through a CLI that bundles it (check with `cratis --version`; CLI 3.100.0 bundles Stage 4.196.0); older versions keep the gap-fill path.
