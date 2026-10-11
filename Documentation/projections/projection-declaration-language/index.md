# Projection Declaration Language

The Projection Declaration Language (PDL) is part of the **Cratis Screenplay** language — the
declarative, indentation-based language for describing Cratis applications. Chronicle uses the
Screenplay compiler to parse projection declarations and turn them into projection definitions.

The full language reference — from-event rules, property mapping, auto-map, keys, event context,
counters, arithmetic, joins, children, nested objects, removal, expressions, and the grammar — lives
in the Screenplay documentation:

- [Screenplay — Projections](/screenplay/projections/)

## Pinned event generations

When you save a PDL projection with the request's `GenerationDelivery` set to `Pinned`, Chronicle
resolves each event reference once and persists the resulting generation in the definition:

| Reference | Pinned definition |
| --- | --- |
| `from PersonRegistered` | Latest registered generation at registration time |

The same rule applies to join and removal references and to clauses inside children and nested
objects. Each clause reads its own pinned generation, even when another clause names a different
generation of the same event type. Registering a new generation does not change an existing pin.
Saving a declaration again resolves its bare names against the generations registered at that time.
PDL has no generation-qualified event reference syntax. For explicit per-clause generations, use the
[.NET projection builders](../declarative/simple-projection.mdx) or model-bound attributes with the
CLR event type representing the desired generation.

`Compatibility` remains the default and retains the previous behavior: bare names mean generation
1 in the definition, while event dispatch matches the event type identifier without its generation.
Ad-hoc queries and immediate projections do not opt into this behavior through the save request.
Wildcard (`all`) projections keep Compatibility; Pinned wildcard registration is not supported.
See [Event type migrations](../../concepts/event-type-migrations.mdx) for delivery and protection rules.

## What remains here

Chronicle-specific client APIs that consume PDL declarations are documented in this section:

- [Ad-hoc Querying](adhoc-querying) — running a PDL declaration ad-hoc with `IProjections.Query()`.
