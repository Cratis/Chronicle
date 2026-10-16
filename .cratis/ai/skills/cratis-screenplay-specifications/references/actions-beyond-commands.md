<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/actions-beyond-commands.md -->
## Actions beyond commands

A slice is not always set off by a command. Name what does:

| Slice | Action |
| --- | --- |
| A view no event builds - its query has a `performer` | `given readmodel` …, `when query <Query>`, `then result` / `then no result` |
| An automation driven by the clock | `given clock "<instant>"`, `when clock "<later instant>"` |
| An automation driven by an application trigger | `when trigger <Trigger>` with the values it carries |
| A translation driven by a capture | `given capture <Capture>` (the record as it was), `when capture <Capture>` (as it is now) |

Use `given clock`, never `given time`, to state the scenario occurrence time and
assert a value mapped from `$context.occurred`. In a command's `produces`, that
mapping executes when the scenario has `given clock`. It is not the same as
`$eventContext.occurred` in a projection, which the execution plan refuses (SKILL.md, "Reference execution — what actually runs").

**Which tools bind these actions.** A compiler that admits ESM v6 binds the clock, trigger
and capture actions, and the `Automation` and `Translate` slices, reactions and captures they
drive; the reference runner executes them through `screenplay test` or MCP `run-specifications`,
with the semantics in [Reactions and cascades](reactions-and-cascades.md). The complete example in
that reference reports `executableReady` with no executable error in `open-workspace` readiness
and `read-workspace` view `executable-diagnostics`.

A compiler that stops before ESM v6 executes `when query` only. The clock, trigger and capture
actions parse, print and are checked against the application, but binding reports `PLAY0268`
naming ESM v6, and an `Automation` or `Translate` slice reports *is not admitted by ESM v1*.
Only binding shows this: `cratis screenplay validate` does not bind, so it stays green.

Write the actions either way - they state what sets the slice off. Report each as
bound and not executed, or as parsed only, naming the tool that said so.
