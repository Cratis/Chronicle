---
title: Cratis Chronicle projection declarations with PDL
description: Write Chronicle projections in the Projection Declaration Language (PDL), using registered read model names and choosing how event generations are resolved.
---
The Projection Declaration Language (PDL) describes how Cratis Chronicle builds read models from
events using text declarations instead of projection classes. PDL is part of Cratis Screenplay,
the indentation-based language for describing Cratis applications. Chronicle uses the Screenplay
compiler to turn those declarations into projection definitions.

Start with [Screenplay — Projections](/screenplay/projections/) for declaration syntax, property
mapping, keys, counters, joins, children, and the full grammar. This page explains the
Chronicle-specific choices that affect whether a declaration compiles and which event generations
it uses. To inspect results without saving a projection, use
[ad-hoc querying](adhoc-querying).

## Graph targets

A projection can build a property graph instead of a read model: write `=> graph <Name>` after the
projection name and describe the `node` and `edge` blocks. Chronicle lowers the declaration into a graph
projection definition and registers the graph as a read model whose schema carries the `x-graph-*`
annotations, the same shape the .NET client registers for a model-bound or fluent graph projection.

```screenplay
graph Organization
  node Person
    personId Uuid key
    name String
  node Group
    groupId Uuid key
    name String
  edge MEMBER_OF source Person target Group
    since DateTime

projection OrganizationGraph => graph Organization
  node Person
    from PersonRegistered key personId
      name = name
    remove with PersonRemoved key personId detach
  node Group
    from GroupCreated key groupId
      name = name
  edge MEMBER_OF
    from MemberAdded source personId target groupId
      since = $eventContext.occurred
    remove with MemberRemoved source personId target groupId
```

Save the graph and its projection together as one declaration. The declaration holds exactly one graph
projection and the graph it builds; top-level `concept` lines are allowed, and a graph property otherwise uses
a primitive type (`Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`). Chronicle stores graph read
models in the graph sink (Neo4j).

- Properties an event shares by name with a node or edge are mapped automatically, as for a model-bound graph.
- An `every` block is applied to each `from` of its block, so a rendered declaration writes the mappings out
  under every `from` instead of in an `every`.
- Previewing a graph projection is not supported.
- Rendering a registered graph projection, whichever client registered it, produces a declaration that
  saves unchanged.

See [Screenplay — Graph projections](/screenplay/projections/graphs/) and [Screenplay — Graphs](/screenplay/graphs/)
for the language.

## Pinned event generations

Set the save request's `GenerationDelivery` to `Pinned` to resolve each event reference to the
latest registered generation when you save. For example, `from PersonRegistered` records the latest
registered generation of `PersonRegistered` in the projection definition. Registering a newer
generation later does not change that pin; saving the declaration again resolves the name against
the generations registered at that time.

The same resolution applies to join and removal references, including clauses inside children and
nested objects. A pinned projection can name only one generation per event type across all its
clauses. Registration rejects conflicting pins, but different event types can pin different
generations.

PDL has no generation-qualified event reference syntax. To name a generation explicitly in code,
use the [.NET projection builders](../declarative/simple-projection.mdx) or model-bound attributes
with the CLR event type representing that generation. The one-generation-per-event-type rule still
applies to pinned projections.

`Compatibility` is the default: bare event names become generation 1 in the definition, while event
dispatch matches the event type identifier without its generation. The save request's setting does
not apply to ad-hoc queries or immediate projections. Wildcard (`all`) projections use
`Compatibility`; `Pinned` wildcard registration is not supported. Read
[Event type migrations](../../concepts/event-type-migrations.mdx) before changing delivery policy
or migrating events used by a pinned projection.

## Read-model identifiers and property names

Use the registered read-model identifier after `=>`, not its display name or schema title. In the
.NET client, that identifier is the full type name (`typeof(TReadModel).FullName`), such as
`MyApp.ReadModels.Order`. Chronicle renders registered projections with this identifier, so you
can save the declaration unchanged.

Match registered spelling and casing exactly in identifiers and property paths. The .NET client's
default naming policy preserves CLR property names, which are normally PascalCase. If a name does
not match exactly, compilation fails. When a display title, short type name, or differently cased
name matches a known identifier or property, the diagnostic suggests the registered spelling.
If several names match, it lists the candidates so you can choose the intended identifier.

## Child-level `every` mappings

An `every` block inside `children` applies its mappings to descendant events too, unless it declares
`exclude children`. This lets a child-level mapping respond to events from deeper in its hierarchy.

For stored declarations created before this behavior was corrected, re-save the original PDL that
uses `every` inside `children`, then replay if recommended. A kernel upgrade or replay alone does
not recompile stored definitions.

## What remains here

Continue with [Ad-hoc Querying](adhoc-querying) to send a declaration through `IProjections.Query()`
and inspect the resulting JSON read models. That API processes a bounded event set without
registering the projection; it is useful for exploration, not a substitute for a maintained read
model.
