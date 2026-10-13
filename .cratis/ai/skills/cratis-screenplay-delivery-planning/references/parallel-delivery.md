<!-- cratis-ai-managed: skills/cratis-screenplay-delivery-planning/references/parallel-delivery.md -->
# Parallel delivery

Read when splitting work across people or agents, building the dependency table or grouping issues.

## Why slices can run in parallel
A slice consumes events and views and produces events and views. Those are its contracts. A slice that needs `OrderConfirmed` can be built and specified against synthetic `given` events before the slice that produces it exists, because the model fixes the event's shape. When the producer lands, the consumer's specifications already describe what it expects. Slice design records this per slice (`cratis-screenplay-slice-design`, `references/slicing.md`); planning reads it.

This holds only for event and view contracts. Edges through `reads` of another slice's view, `invokes` of another slice's command from a reaction, and shared streams are real coordination points: record them as edges and plan an owner for each.

## Derive the dependency table
Use the Screenplay MCP rather than a board or memory (`cratis-screenplay-model-authoring`):
- `describe-application` for slice counts and children,
- `dependency-graph` at slice level (`from`/`to`) with its `edges`, `cycles` and `order` views as the starting point for the waves; its order is advisory and covers only `usesFactsFrom`/`reactsTo`/`decidesFrom` edges (`reads` maps to `decidesFrom`; only `asks` edges from invokes, actions and forms, and `shows` edges are left out of order and cycles),
- `dependencies` and `find-references` for each slice's consumed and produced contracts and for the `reads` and `invokes` edges; `invokes`, action and form edges are not ordered by the graph, and `reads` is ordered but still a coordination point.

`complete: false`, uncovered declarations or ambiguous bindings mean the table is incomplete; list them as unknown and resolve them in the model before relying on parallelism. Container `depends on` declarations are orientation, not execution order.

Table, one row per slice:

| Slice | Type | Realization | Consumes (from slice) | Produces | Waits on | Wave |
|---|---|---|---|---|---|---|

## Waves
- Wave 1: slices that consume nothing, or only events their own scope produces.
- Wave n: slices whose producers are all in earlier waves.
- Slices in one wave are independent and can run in parallel.
- A cycle means feedback; model it as an Automation slice or return to slice design. Do not schedule a cycle.

Because consumers can build against synthetic events, a wave boundary is an integration check (producer and consumer agree on the contract), not a hard start gate. Say which waves you started early and where the contract check will happen.

## Contract ownership
- Each event and view has one owning slice; its shape is changed in the model, never in a consumer.
- A contract change reopens every consumer in the table: recount and notify their owners.
- When people or agents work in parallel, brief each from the model's slice and its consumed contracts (a brief states the goal, the files owned, the exact verification command, the done condition, the stop conditions and the report format); one writer per branch or worktree.

## Slice groups and issues
- One GitHub issue per slice group: slices that share a view, a stream or an owner, small enough for one person to finish.
- Status block lists the slices (name, type, realization), the wave, blockers and each slice's planning status (todo, in progress, blocked, done). That vocabulary is per slice; the issue's own state follows the repository's hygiene (in progress, in review, paused, blocked, done when delivered). Update the block rather than adding progress comments.
- Claim before starting: assign the issue, keep an in-progress status in its body and check for an existing claim (for example `issue_work` where the harness provides it). If the group is already in progress, choose another; do not take over.
- Mark `blocked` only with the named producer, decision or outside party.

## Parallelism that does not pay
- Slices writing the same file, stream or external resource.
- Two hand-written slices that depend on the same unresolved integration.
- A wave with one slice: no parallelism, use the time for integration, review or the next wave's briefs.
