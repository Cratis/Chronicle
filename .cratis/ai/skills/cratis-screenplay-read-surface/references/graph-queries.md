<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/graph-queries.md -->
# Graph queries

A graph query is a query: it has a name, filters, and returns a read model. Only its header and
body differ.

Requires Screenplay 5.8.0 or later (`screenplay --version`).

- The header ends in `graph <Graph>` instead of reading a stored read model:
  `query MembersOf => Membership[] graph Organization`.
- `filter <name> <Type>` lines declare the arguments; `where` compares a node or edge property
  with `$<name>` or a literal.
- `match` / `optional match` give the path and `return` fills the read model, one assignment per
  required property.
- The result read model is an ordinary `readmodel`; it is not a node or an edge, and no
  projection builds it. A screen's `data` line binds the query like any other.

Do not restate the grammar here. The complete model, the rules and `PLAY0878`–`PLAY0885` are in
[reading a graph with a query](../../cratis-screenplay-projections/references/graphs.md#reading-a-graph-with-a-query);
the specification step is in
[graph steps](../../cratis-screenplay-specifications/references/graph-steps.md#asserting-a-graph-query).
Choose a graph query for a path question; a lookup by key stays a document read model with a
projection.
