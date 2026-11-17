<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/graph-steps.md -->
# Graph specification steps

## Contents

- Steps
- Example
- Asserting a graph query
- Asserting collected rows
- Rules

## Steps

| Step | Meaning |
| --- | --- |
| `has node <Label> <key>` | The node exists and the properties listed under it hold the given values. |
| `has edge <TYPE> from <source> to <target> [identified by <value>]` | The edge exists and the properties listed under it hold the given values. |
| `no node <Label> <key>` | The node does not exist. `then graph` only. |
| `no edge <TYPE> from <source> to <target> [identified by <value>]` | The edge does not exist. `then graph` only. |
| `then query <Name>` | Runs a graph query and compares its `result` rows; see below. |
| `given clock "<instant>"` | Fixes `$eventContext.occurred` for the events the specification appends. |

## Example

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

      specification RemovingAPersonDetachesTheirEdges
        given graph Organization
          has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
            name = "Ada Lovelace"
          has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
          has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
        when append PersonRemoved
          for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
        then graph Organization
          no node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
          no edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
          has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
```

The specification sits in the slice that declares the graph and its projection; they give
the steps their labels and keys. Check with `screenplay --warnaserror` and `screenplay test`.

## Asserting a graph query

Requires Screenplay 5.8.0 or later (`screenplay --version`). A query that ends in `graph <Graph>`
(see [reading a graph](../../cratis-screenplay-projections/references/graphs.md#reading-a-graph-with-a-query))
is asserted with `then query <Name>`, after `given graph` or after events a projection turns into
nodes and edges. `given clock` is stated once, before the events:

```screenplay
concept PersonId : Uuid
concept GroupId : Uuid
concept Name : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      event PersonRegistered
        name Name

      event GroupCreated
        title Name

      event MemberAdded
        groupId GroupId

      graph Organization
        node Person
          personId PersonId key
          name     Name
        node Group
          groupId GroupId key
          title   Name
        edge MEMBER_OF source Person target Group
          since DateTime optional

      projection OrganizationChart => graph Organization
        node Person
          from PersonRegistered key $eventSourceId
            name = name
        node Group
          from GroupCreated key $eventSourceId
            title = title
        edge MEMBER_OF
          from MemberAdded source $eventSourceId target groupId
            since = $eventContext.occurred

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

      specification MembersAreListed
        given clock "2026-10-05T07:00:00Z"
        given PersonRegistered
          name = "Ada Lovelace"
          for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
        given GroupCreated
          title = "Analysts"
          for "0f8fad5b-d9cb-469f-a165-70867728950e"
        given MemberAdded
          groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
          for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
        then query MembersOf
          arguments
            groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
          result
            personId = "6fa459ea-ee8a-4ca4-894e-db77e160355e"
            person = "Ada Lovelace"
            since = "2026-10-05T07:00:00Z"
```

## Asserting collected rows

Requires Screenplay 6.29.0 or later (`screenplay --version`). A query that `collect`s nested rows
(see [collect nested rows](../../cratis-screenplay-projections/references/graphs.md#collect-nested-rows))
is asserted under `then query` with structured values (an indented `item` block per row; Screenplay has no JSON, `PLAY0833`): the collected
property on its own line, one `item` block per row, in order; `<property> none` asserts an empty
list and `<property> = null` an absent optional row. List rows compare in order: the reached node's
key, then the edge's `identified by` value.

## Rules

- Keys are the values of the node's `key` property, written as literals.
- A `has` step checks only the properties listed under it.
- A malformed step is `PLAY0875`; an undeclared graph, node label or edge type is
  `PLAY0876`; a property the node or edge does not declare is `PLAY0868`.
- The reference runner keeps an in-memory graph and applies the five operations of the
  projection.
- `$eventContext.occurred` is materialized. Without `given clock` the runner uses a fixed
  instant, so state the clock whenever an asserted node or edge property is mapped from it.
