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
objects. A pinned projection can name only one generation per event type across all its clauses;
registration rejects conflicting pins, including those in children and nested objects. Different
event types can pin different generations. Registering a new generation does not change an existing pin.
Saving a declaration again resolves its bare names against the generations registered at that time.
PDL has no generation-qualified event reference syntax. For explicit per-clause generations, use the
[.NET projection builders](../declarative/simple-projection.mdx) or model-bound attributes with the
CLR event type representing the desired generation.

`Compatibility` remains the default and retains the previous behavior: bare names mean generation
1 in the definition, while event dispatch matches the event type identifier without its generation.
Ad-hoc queries and immediate projections do not opt into this behavior through the save request.
Wildcard (`all`) projections keep Compatibility; Pinned wildcard registration is not supported.
See [Event type migrations](../../concepts/event-type-migrations.mdx) for delivery and protection rules.

## Read-model identifiers and property names

The target after `=>` is the registered read-model identifier, not its display name or schema
title. For the .NET client, this is the full type name (`typeof(TReadModel).FullName`), such as
`MyApp.ReadModels.Order`. Chronicle renders registered projections with that identifier so you
can save the declaration unchanged.

Identifiers and property paths must match their registered spelling and casing exactly. The
.NET client's default naming policy uses PascalCase properties. A missing name remains a compiler
error; when a title, short type name, or differently cased name matches a known identifier or
property, the diagnostic suggests the registered spelling. Ambiguous matches list the candidates.

## What remains here

Chronicle-specific client APIs that consume PDL declarations are documented in this section:

- [Ad-hoc Querying](adhoc-querying) — running a PDL declaration ad-hoc with `IProjections.Query()`.
