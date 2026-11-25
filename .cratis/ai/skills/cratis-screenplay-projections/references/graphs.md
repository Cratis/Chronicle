<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/graphs.md -->
# Graph declarations and graph projections

## Contents

- Declaring a graph
- Projecting events onto it
- One element per item of a collection (`foreach`)
- Storing a graph in a named sink
- Reading the graph backwards
- Reading a graph with a query
- Collect nested rows
- The five operations
- What a graph projection cannot say
- Diagnostics

## Declaring a graph

A slice-level `graph`, a sibling of `readmodel`. Each `node` has typed properties and
exactly one `key`; each `edge` names existing node labels and may add properties.

```screenplay
concept PersonId : Uuid
concept GroupId : Uuid
concept PersonName : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      graph Organization
        node Person
          personId PersonId key
          name     PersonName
        node Group
          groupId GroupId key
          name    String
        edge MEMBER_OF source Person target Group
          since DateTime
```

- A property is a primitive, a concept over one, or an array of those. A nested
  object, a `type` or a read model is `PLAY0863`: model it as another node and an edge.
- An edge is identified by type, source key and target key, so a repeated
  `MEMBER_OF` between one pair is the same edge. For several edges of one type
  between the same pair, write `edge HAS_ROLE source Person target Group identified by assignmentId`
  and declare `assignmentId` on the edge (`PLAY0871`).
- Edge types read best in `UPPER_SNAKE_CASE`.

## Projecting events onto it

Declared in the slice next to its graph; a standalone projection document cannot
hold one (`PLAY0877`). The model below is complete: events, graph and projection in one slice.

```screenplay
concept PersonId : Uuid
concept GroupId : Uuid
concept PersonName : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      event PersonRegistered
        name PersonName

      event PersonRemoved

      event GroupCreated
        name String

      event MemberAdded
        groupId GroupId

      event MemberRemoved
        groupId GroupId

      graph Organization
        node Person
          personId PersonId key
          name     PersonName
        node Group
          groupId GroupId key
          name    String
        edge MEMBER_OF source Person target Group
          since DateTime

      projection OrganizationChart => graph Organization
        node Person
          from PersonRegistered key $eventSourceId
            name = name
          remove with PersonRemoved key $eventSourceId detach
        node Group
          from GroupCreated key $eventSourceId
            name = name
        edge MEMBER_OF
          from MemberAdded source $eventSourceId target groupId
            since = $eventContext.occurred
          remove with MemberRemoved source $eventSourceId target groupId
```

- `from <Event> key <expr>` creates or updates a node; `every` applies mappings to
  every `from` of the block; `remove with ... key <expr> [detach]` deletes it.
- An edge `from` takes `source <expr> target <expr>`; with `identified by` on the
  edge, every handler writes `identified by <expr>` after `target`.
- Mappings, counters (`increment`, `decrement`, `count`, `add ... by`, `subtract ... by`),
  `$eventContext` and AutoMap behave as in a read-model projection. A property the
  node or edge does not declare is `PLAY0868`.
- An edge `from` creates a missing endpoint as a bare node, so an event may draw an
  edge before the events that create its nodes arrive. A projection owns the edges
  and node properties it declares and never deletes a node it did not declare.

## One element per item of a collection (`foreach`)

An event that carries a list can draw one node or edge per item. Write `foreach <collection>` after
the event name in `from` or `remove with` (`from <Event> foreach <collection> key ...`, `from <Event>
foreach <collection> source ... target ...`). Requires Screenplay 5.10.0 or later
(`screenplay --version`); a model that uses it selects ESM version 41.

```screenplay
type ModelSlot
  slot   String
  series Uuid

module Scoring
  feature Models
    slice StateView ModelScores
      event ModelDefined
        slots ModelSlot[]

      event ModelRetired
        slots ModelSlot[]

      graph Scoring
        node Model
          modelId Uuid key
        node Series
          seriesId Uuid key
        edge SCORES source Model target Series identified by slot
          slot String

      projection ModelScores => graph Scoring
        edge SCORES
          from ModelDefined foreach slots source $eventSourceId target slots.series identified by slots.slot
            slot = slots.slot
          remove with ModelRetired foreach slots source $eventSourceId target slots.series identified by slots.slot
```

- The handler runs once per item. An item's properties are written `<collection>.<property>`
  (`slots.series`); every other expression is the event's (`$eventSourceId`).
- The collection is a property of the event that is a list of a composite type; anything else, and
  a collection inside another (`foreach slots.parts`), is `PLAY0886`. Iteration goes one level deep.
- An absent or empty list does nothing.
- Chronicle runs the handler once per item (Chronicle 19.134 and later).

## Storing a graph in a named sink

A graph names no sink by default and is stored in the application's default graph sink. To keep one
graph apart from another, declare a sink `for graphs` at the application root and put a bare
`sink <Name>` line in the graph body. The projection never names a sink: it writes to its graph's
sink. Requires Screenplay 6.4.0 (`screenplay --version`).

```screenplay
sink People for graphs

concept PersonId : Uuid
concept GroupId : Uuid
concept PersonName : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      event PersonRegistered
        name PersonName

      event GroupCreated
        name String

      event MemberAdded
        groupId GroupId

      graph Organization
        sink People
        node Person
          personId PersonId key
          name     PersonName
        node Group
          groupId GroupId key
          name    String
        edge MEMBER_OF source Person target Group
          since DateTime

      projection OrganizationChart => graph Organization
        node Person
          from PersonRegistered key $eventSourceId
            name = name
        node Group
          from GroupCreated key $eventSourceId
            name = name
        edge MEMBER_OF
          from MemberAdded source $eventSourceId target groupId
            since = $eventContext.occurred
```

A graph body has no properties of its own, so every `sink` line in it is the directive: an
undeclared name is `PLAY0933`, and a sink declared `for readmodels` is `PLAY0934`. The model never
names a Neo4j server or database; the deployment maps `People` to one. The read-model side and the
full `PLAY0931`–`PLAY0939` table are in the read surface's
[sinks](../../cratis-screenplay-read-surface/references/sinks.md).

## Reading the graph backwards

1. List the questions the graph must answer ("who is in this group, including nested groups?").
2. Write each as a path: `Person -MEMBER_OF-> Group -PART_OF-> Group`.
3. Each hop is an edge type; each stop is a node label with a key.
4. For each edge type and node label, name the event that creates it and the one
   that removes it. Nothing creates it: add the fact to the event model, not a write
   to the graph.
5. Add `detach` to any node removal whose label has attached edges.
6. A question that is a lookup by key, not a path, belongs in a document read model.
7. Write each path question as a `query` ending in `graph <Graph>`; see
   [Reading a graph with a query](#reading-a-graph-with-a-query).

## Reading a graph with a query

A query that ends in `graph <Graph>` follows a path over the graph's declared nodes and edges and
returns an ordinary read model. Requires Screenplay 5.8.0 or later (`screenplay --version`).

```screenplay
concept PersonId : Uuid
concept GroupId : Uuid
concept Name : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      graph Organization
        node Person
          personId PersonId key
          name     Name
        node Group
          groupId GroupId key
          title   Name
        edge MEMBER_OF source Person target Group
          since DateTime optional

      readmodel Membership
        personId PersonId key
        person   Name
        since    DateTime optional

      query MembersOf => Membership[] graph Organization
        filter groupId GroupId
        match person:Person -link:MEMBER_OF-> group:Group
        where group.groupId = $groupId
        return
          personId = person.personId
          person   = person.name
          since    = link.since
```

- `match` is a path of `alias:Label`, `-alias:TYPE->` (outgoing) and `<-alias:TYPE-` (incoming)
  steps; an alias is needed only where `where` or `return` names it.
- `optional match` follows a path that may be absent, so what it returns must go to optional
  read-model properties (`PLAY0883`).
- `where` joins equality tests with `and`; each compares a node or edge property with a
  `$argument` the query declares or a literal.
- `return` assigns exactly the read model's required properties, each from a node or edge
  property of the pattern with the same type (`PLAY0881`).
- Variable-length paths, `or`, ranges, functions and aggregates do not exist (`PLAY0885`); there
  is no engine-native query text.
- A hop must use an edge that connects those two labels in that direction (`PLAY0880`); an
  alias is used once (`PLAY0884`).

The read-surface side is in
[graph queries](../../cratis-screenplay-read-surface/references/graph-queries.md); assert a graph
query in a specification with `then query`
([graph steps](../../cratis-screenplay-specifications/references/graph-steps.md)).

## Collect nested rows

Requires Screenplay 6.29.0 or later (`screenplay --version`); a collecting query selects ESM v54.

A query returns nested rows with a `return` assignment of the form
`<property> = collect <root> -[alias:]TYPE-> [alias:]Label` (or `<-[alias:]TYPE-` for an incoming
edge), followed by one line per row property:

```screenplay
concept PersonId : Uuid
concept GroupId  : Uuid
concept Name     : String

type GroupMembership
  id    GroupId
  name  Name
  since DateTime optional

type GroupSummary
  id   GroupId
  name Name

module Organization
  feature Chart
    slice StateView OrganizationViews
      graph Organization
        node Person
          personId PersonId key
          name     Name
        node Group
          groupId GroupId key
          name    Name
        edge MEMBER_OF source Person target Group
          since DateTime optional
        edge PART_OF source Group target Group

      readmodel PersonWithGroups
        id     PersonId key
        name   Name
        groups GroupMembership[]

      readmodel GroupWithParent
        id     GroupId key
        name   Name
        parent GroupSummary optional

      query PeopleInGroup => PersonWithGroups[] graph Organization
        filter groupId GroupId
        match person:Person
        where group.groupId = $groupId
        return
          id     = person.personId
          name   = person.name
          groups = collect person -membership:MEMBER_OF-> group:Group
            id    = group.groupId
            name  = group.name
            since = membership.since

      query GroupsWithParent => GroupWithParent[] graph Organization
        match group:Group
        return
          id     = group.groupId
          name   = group.name
          parent = collect group -PART_OF-> parent:Group
            id   = parent.groupId
            name = parent.name
```

- **The root.** The query reads like a view: one `match <alias>:<Label>` of its root alone, no hops
  and no `optional match`. Scalar returns come from the root; each result row is one root node,
  distinct by key and ordered by the key (`PLAY1147` otherwise).
- **The edge.** A collect follows one declared edge from the root, outgoing or incoming; the arrow
  settles the direction, so an edge whose source and target share a label (`PART_OF`) is never
  ambiguous. Name the edge (`-membership:MEMBER_OF->`) to read one of its properties.
- **The rows.** The property is a list of a composite type (`GroupMembership[]`) or an optional one
  (`GroupSummary optional`), and the lines under the collect assign every required property of the
  type (`PLAY1145`). A list has one row per edge reached, ordered by the reached node's key and then
  the edge's `identified by` value, and is empty when nothing is reached; an optional row is the
  first of those, or null.
- **Conditions select roots.** A `where` on a collected node keeps the roots with at least one
  reached node that satisfies every condition on that alias; it never narrows what is collected. A
  condition on a collected edge is `PLAY1147`.
- **Absent properties.** A node created only by an edge has none of its other properties yet; a row
  reads them as null.
- **One level.** A collect inside a collect is `PLAY1146`; a malformed collect is `PLAY1144`.

Assert collected rows in a specification with structured values: see
[graph steps](../../cratis-screenplay-specifications/references/graph-steps.md#asserting-collected-rows).
Stage may not render a collecting query; see the ESM v54 row in
[language admission](../../cratis-screenplay-toolchain/references/language-admission.md).

## The five operations

Every sink applies the same idempotent operations: `UpsertNode`, `SetProperties`,
`DeleteNode` (with or without `detach`), `UpsertEdge`, `DeleteEdge`. Nodes are
identified by label and key; edges by type, source, target and the `identified by` value.

## What a graph projection cannot say

`children`, `nested`, `join`, `all`, `clear with`, `variant`, a
projection-level `key` or `sequence`, and `automap` blocks are `PLAY0872`;
`remove via join` is read as a malformed removal and is `PLAY0873`. There is
no engine-native block: whatever the language cannot say is not supported. The event
model board, find-references and rename do not follow graph projections, so renaming
an event leaves a graph projection reported as unknown.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `PLAY0862` | Malformed graph, node or edge declaration |
| `PLAY0863` | Property type not a primitive, concept or array of those |
| `PLAY0864` | Node with no `key` or more than one |
| `PLAY0865` | Edge names an undeclared source or target label |
| `PLAY0866` | Projection targets an undeclared graph |
| `PLAY0867` | Block for a label or edge type the graph does not declare |
| `PLAY0868` | Mapped, identity or asserted property the node or edge lacks |
| `PLAY0869` | `detach` on an edge |
| `PLAY0870` | Warning: node removal without `detach` while edges attach to the label |
| `PLAY0871` | `identified by` names a property the edge does not declare |
| `PLAY0872` | Read-model-only construct in a graph projection |
| `PLAY0873` | Malformed block or handler, missing `key` or `source`/`target` |
| `PLAY0874` | Duplicate declaration or duplicate node/edge build in one projection |
| `PLAY0875` | Malformed `given graph` / `then graph` step |
| `PLAY0876` | Specification names an undeclared graph, label or edge type |
| `PLAY0877` | Graph projection declared outside a slice |
| `PLAY0878` | Malformed graph query: header, `match` pattern, `where` condition or `return` block |
| `PLAY0879` | Graph query names an undeclared graph, node label, edge type, alias or property |
| `PLAY0880` | Graph query hop uses an edge that does not connect those labels in that direction |
| `PLAY0881` | Graph query does not return exactly the read model's required properties, or returns the wrong type |
| `PLAY0882` | Graph query condition uses an undeclared argument or compares different types |
| `PLAY0883` | Optional-match value assigned to a read-model property that is not optional |
| `PLAY0884` | Graph query uses the same alias twice |
| `PLAY0885` | Graph query uses variable-length paths, `or`, ranges, functions or aggregates |
| `PLAY0886` | `foreach` names a collection inside another, or something that is not a list of a composite type of the event |
| `PLAY1144` | Malformed `collect` (shape, start at the root, row lines) |
| `PLAY1145` | `collect` does not fit the read model: not `Row[]` or `Row optional`, or a row property unknown, duplicate, missing or mistyped |
| `PLAY1146` | `collect` nested inside another `collect` |
| `PLAY1147` | Collecting query is not view-shaped, or has a `where` condition on a collected edge |
