<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/routes-and-external-operations.md -->
# Routes and external operations

This reference holds the route, public-event and external-operation guidance for command productions.

## Routes and external operations

Routes bind as ESM v8; production `stream Source.Stream` overrides
select v10. Read toolchain [routes](../../cratis-screenplay-toolchain/references/production-routes-and-filters.md)
and [source/stream declarations](../../cratis-screenplay-toolchain/references/sources-and-streams.md).
A stored stream `All` / `id "All"` refuses PLAY0273;
rename and history-migration guidance is in that reference.
`public event X` is a local export; `event X from "store"` is a locally declared,
unverified foreign shape. Commands never produce public events. Read
[translation direction](../../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md)
for the released ESM v9 boundary and quoted opaque origins.

Use `system`, `uses` and inline/standalone `operation` declarations to state
external work in the authored production sequence, with execute/compensate phase
intent. These remain unadmitted (PLAY0268), not post-commit reactions. Read
[operation intent](operations.md) for forms, boundaries and checked
failure/compensation specification syntax.
