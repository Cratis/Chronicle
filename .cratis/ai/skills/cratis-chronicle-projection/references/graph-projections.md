<!-- cratis-ai-managed: skills/cratis-chronicle-projection/references/graph-projections.md -->
# Graph projections

## Contents

- Model-bound
- Fluent
- Counters and edge identity
- The operations
- Redelivery and replay
- Sinks
- Testing and analyzers

A **node** has a label (the type name), a unique key and properties. A property is a
primitive or an array of primitives; an object property is an error, so model the
object as a node and connect it with an edge. An **edge** has a type, a source node,
a target node and properties. By default it is identified by type, source and target.
Name the graph with `[Graph("Organization")]` on the types or the assembly; without
it the graph is named after the assembly's root namespace.

## Model-bound

`[Key]` marks a node's unique key. `[Source<>]` and `[Target<>]` on an edge mark the
members holding the end keys; they are not properties of the edge. The node key is
the event source id unless `[FromEvent<>]` names an event property.

```csharp
[Graph("Organization")]
[Node]
[FromEvent<PersonRegistered>]
[RemovedWith<PersonRemoved>(detach: true)]
public record Person([Key] Guid PersonId, string Name);

[Graph("Organization")]
[Node]
[FromEvent<GroupCreated>]
public record Group([Key] Guid GroupId, string Name);

[Graph("Organization")]
[Edge("MEMBER_OF")]
[EdgeFrom<MemberAdded>(sourceKey: nameof(MemberAdded.PersonId), targetKey: nameof(MemberAdded.GroupId))]
[EdgeRemovedWith<MemberRemoved>(sourceKey: nameof(MemberRemoved.PersonId), targetKey: nameof(MemberRemoved.GroupId))]
public record MemberOf(
    [Source<Person>] Guid PersonId,
    [Target<Group>] Guid GroupId,
    [SetFromContext<MemberAdded>(nameof(EventContext.Occurred))] DateTimeOffset Since);
```

Properties map by name from an event property of the same name, or with the
attributes of read-model projections: `[SetFrom<>]`, `[SetFromContext<>]`,
`[Increment<>]`, `[Decrement<>]`, `[Count<>]`, `[AddFrom<>]`, `[SubtractFrom<>]`.

## Fluent

`IGraphProjection` builds exactly the same definition as the attributes:

```csharp
[Graph("Organization")]
public class OrganizationGraph : IGraphProjection
{
    public void Define(IGraphProjectionBuilder builder) => builder
        .Node<Person>(n => n
            .IdentifiedBy(p => p.PersonId)
            .From<PersonRegistered>()
            .RemovedWith<PersonRemoved>(b => b.Detach()))
        .Node<Group>(n => n
            .IdentifiedBy(g => g.GroupId)
            .From<GroupCreated>())
        .Edge<MemberOf, Person, Group>(e => e
            .From<MemberAdded>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)
                .Set(m => m.Since).ToEventContextProperty(c => c.Occurred))
            .RemovedWith<MemberRemoved>(b => b
                .UsingSourceKey(x => x.PersonId)
                .UsingTargetKey(x => x.GroupId)));
}
```

Chronicle also compiles the projection declaration language form (`graph Organization` with
`node` and `edge` declarations, then `projection X => graph Organization`), which is the text
`cratis-screenplay-projections` produces for a model.

## Counters and edge identity

Counters work on node and edge properties:
`[Increment<MemberAdded>] [Decrement<MemberRemoved>] int MemberCount`.

Give an edge a `[Key]` member to allow several edges of one type between the same
two nodes. Every mapping of that edge must then say which event property holds the
identity (`identity:` argument or `UsingIdentity`). A sink that cannot hold several
edges between one pair rejects such an edge type.

```csharp
[Edge("VISITED")]
[EdgeFrom<PlaceVisited>(
    sourceKey: nameof(PlaceVisited.PersonId),
    targetKey: nameof(PlaceVisited.PlaceId),
    identity: nameof(PlaceVisited.VisitId))]
public record Visited(
    [Source<Person>] Guid PersonId,
    [Target<Place>] Guid PlaceId,
    [Key] Guid VisitId,
    [Count<PlaceVisited>] int Times);
```

## The operations

Each event yields a `GraphChangeset` of five idempotent operations: `UpsertNode`,
`SetProperties` (nothing is created), `DeleteNode` (with `detach` the attached edges
go too; without it a node that still has edges cannot be deleted), `UpsertEdge` and
`DeleteEdge` (the end nodes stay).

- An edge operation creates a bare end node when it does not exist; a node mapping
  completes it later.
- A projection owns the edges and node properties it maps and never deletes a node it did not declare.
- Counters carry absolute values: the engine reads the current value from the sink
  and the operation carries the result.

## Redelivery and replay

Every node and edge stores a **watermark**, the sequence number of the event that last
wrote it. A write at or below the watermark is skipped, so redelivered and out-of-order
events do not apply twice. Replay is **mark and sweep**: a replay starts a new
generation for the projection, elements it has not rewritten read as absent (counters
rebuild from zero), and at the end elements it did not write are deleted. Elements
other projections wrote, and nodes only edges created, are never swept.

## Sinks

A graph is stored by a graph sink, set per client with `DefaultGraphSinkTypeId`
(default `Neo4j`; `WellKnownSinkTypes.InMemoryGraph` keeps it in the kernel's memory).
The Neo4j sink uses one database per event store and namespace, with a single-database
mode for development; Cratis Arc reads those databases with `cratis-arc-neo4j`.

```csharp
services.Configure<ChronicleOptions>(options =>
{
    options.DefaultGraphSinkTypeId = WellKnownSinkTypes.InMemoryGraph;
});
```

To keep one graph in its own Neo4j server, name a sink configured in the Kernel's `sinks` section
(`cratis-chronicle-hosting`) with `[Sink("name")]` from `Cratis.Chronicle.Sinks`. Put it on an
`IGraphProjection` class, or on **every** node and edge of a model-bound graph: elements of one
graph naming different sinks fail registration with `GraphHasConflictingSinks`. Without it the
graph uses `ChronicleOptions.DefaultGraphSinkName`, then the Kernel's `sinks.graphs`, then its
`neo4j` server. Requires Chronicle 19.144.0.

```csharp
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Sinks;

[Graph("People")]
[Sink("people")]
public class PeopleGraph : IGraphProjection
{
    public void Define(IGraphProjectionBuilder builder)
    {
        // Nodes and edges, as in the fluent graph projection.
    }
}
```

A graph naming a read-model sink (`MongoDB` or `SQL`) is refused with
`SinkConfigurationIsForAnotherKindOfReadModel`. Changing a graph's sink replays it into the new one.

## Testing and analyzers

`GraphScenario` runs a graph projection against the in-memory graph sink:

```csharp
using var scenario = new GraphScenario();

await scenario.Append(personId, new PersonRegistered(personId, "Ada"));
await scenario.Append(personId, new MemberAdded(personId, groupId));

var person = await scenario.Node<Person>(personId);
var membership = await scenario.Edge<MemberOf>(personId, groupId);
var memberships = await scenario.Edges<MemberOf>();
```

- CHR0066: a node or an edge has an object property.
- CHR0067: a node does not have exactly one key, or an edge has more than one.
- CHR0068: a type is both a node or an edge and a read model.
