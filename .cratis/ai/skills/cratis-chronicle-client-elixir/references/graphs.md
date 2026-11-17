<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/graphs.md -->
# Graph projections with the node and edge DSL

## Contents

- Declaring a graph
- Options
- Counters and edge identity
- Compiler checks
- Registering and choosing the sink
- Not supported yet

## Declaring a graph

```elixir
defmodule MyApp.Graphs.Person do
  use Chronicle.Graphs.Node
  defstruct person_id: "", name: ""
end

defmodule MyApp.Graphs.Group do
  use Chronicle.Graphs.Node
  defstruct group_id: "", name: ""
end

defmodule MyApp.Graphs.MemberOf do
  use Chronicle.Graphs.Edge, type: "MEMBER_OF"
  defstruct since: ""
end

defmodule MyApp.Graphs.Organization do
  use Chronicle.Graphs.Graph, id: "Organization"

  node MyApp.Graphs.Person, key: :person_id do
    from MyApp.Events.PersonRegistered, key: :person_id, set: [name: :name]
    removed_with MyApp.Events.PersonRemoved, key: :person_id, detach: true
  end

  node MyApp.Graphs.Group, key: :group_id do
    from MyApp.Events.GroupCreated, key: :group_id
  end

  edge MyApp.Graphs.MemberOf, source: MyApp.Graphs.Person, target: MyApp.Graphs.Group do
    from MyApp.Events.MemberAdded,
      source_key: :person_id,
      target_key: :group_id,
      set: [since: {:event_context, :occurred}]

    removed_with MyApp.Events.MemberRemoved, source_key: :person_id, target_key: :group_id
  end
end
```

The struct fields are the properties, typed by their `defstruct` defaults; a property
is a primitive or a list of primitives. A node's label and an edge's type default to the
last segment of the module name (`label:` / `type:` choose another). Property names are
the `camelCase` of the field names, as in the .NET client, so both register the same definition.
`node/3` names the unique key field with `key:`; `edge/3` names its `source:` and
`target:` nodes, both declared with `node` in the same graph. The end nodes are not
properties of the edge.

## Options

- `from/2`: a node takes `key:`, an edge `source_key:` and `target_key:` (event
  properties; each defaults to the event source id). `update_only: true` on a node's
  `from` changes the node only when it exists.
- `removed_with/2`: deletes the node or edge. A node accepts `detach: true` (deletes
  its edges too; without it a node that still has edges cannot be deleted). An edge
  cannot detach.
- Property options as for read models: `set:` (event property, `:event_source_id`,
  `{:event_context, :occurred}`, `{:value, v}`), `add:`, `subtract:`, `increment:`,
  `decrement:`, `count:`, `clear:`.

## Counters and edge identity

```elixir
node MyApp.Graphs.Team, key: :team_id do
  from MyApp.Events.TeamFormed, key: :team_id
  from MyApp.Events.TeamMemberJoined, key: :team_id, increment: :member_count
  from MyApp.Events.TeamMemberLeft, key: :team_id, decrement: :member_count
end
```

`identified_by:` on an edge names a struct field that distinguishes several edges of
one type between the same two nodes. Every `from` and `removed_with` of that edge
must then name the identity event property with `identity:` or have one of the same
name as the field. A sink that cannot hold several edges between one pair rejects it.

```elixir
edge MyApp.Graphs.Visited,
  source: MyApp.Graphs.Customer,
  target: MyApp.Graphs.Shop,
  identified_by: :visit_id do
  from MyApp.Events.ShopVisited, source_key: :customer_id, target_key: :shop_id, count: :times
end
```

## Compiler checks

A graph that cannot work raises a `CompileError` naming the declaration line when an
edge names a source or target that is not a node of the graph; a node has no `key:` or
the key is not a struct field; an edge's `removed_with` asks to `detach`; a node or edge
struct has a nested struct or map property; an identified edge has an event with no
identity; or a mapped field, option or module is unknown.

## Registering and choosing the sink

```elixir
{Chronicle.Client,
  connection_string: "chronicle://localhost:35000",
  event_store: "my-store",
  default_graph_sink_type_id: :in_memory_graph,
  graphs: [MyApp.Graphs.Organization]}
```

The client registers the graph as a read model stored by the graph sink and as the
projection that fills it, with `id:` as read model name and projection identifier.
`:default_graph_sink_type_id` defaults to `:neo4j`; use `:in_memory_graph` for development
and tests. `:default_sink_type_id` still chooses the sink of document read models only.

## Not supported yet

The client does not read a graph (use the engine driver) and `Chronicle.Testing` has no
graph scenario, so test a graph by asserting the definition it registers.
