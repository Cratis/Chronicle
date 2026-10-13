<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/model-inventory-report.md -->
# Model inventory report

Use this for a read-only "what is in this model and where are the gaps" report on an
existing, extracted or inherited `.play` model, before or without a full checklist run. It is
the inventory the audit pass in `SKILL.md` produces, in a fixed shape a maintainer can read
in a minute. It never changes the model. Adapted from Martin Dilger and Nebulit GmbH's
analyze-existing-model (see `provenance.md`), with board, status and placement steps replaced by
the Screenplay MCP views.

## Gather (read-only)
1. Scope. A named module narrows the report to that module; otherwise report the whole
   model, one block per module. If the named module does not exist, say so and list the modules
   that do.
2. Structure: `describe-application` (view `summary`) gives modules, features and slices by kind.
3. Elements: counts of `event`, `command`, `readmodel`, `screen`, `reaction` and `trigger`
   per module from `search-declarations`; fall back to the source files when a view is
   unavailable and say which.
4. Specifications: `find-assertion-gaps` for slices without an assertion; `find-specification-obligations`
   for obligations with no matching specification. These read authored examples, not executed
   coverage (`../../cratis-screenplay-model-authoring/references/mcp-tools.md`).
5. Gaps and orphans: `find-references` and `dependencies` for declarations referenced by nothing;
   `find-modeling-smells` for the candidate shapes.

## Report
```
## Model inventory: <model root or module>
Inspected: <date>, Screenplay <tool version>, source identity <revision>

### Modules (n)
- <module>: <n> features, <n> slices

### Elements (per module)
| Kind | Count |
|---|---|
| event | |
| command | |
| readmodel | |
| screen | |
| reaction / trigger | |
| capture | |

### Specification coverage
- n of total slices have at least one assertion (pct)
- Without: <names, at most 10, then "and N more">

### Structural gaps (per slice)
<slice: what is missing>, or "none"

### Orphans
<declarations nothing references>, or "none"

### Shapes
<left chair, right chair, bed, shelf only as candidates that held up against the domain; see
anti-patterns.md>, or "none"

### Summary
<2 to 4 sentences: maturity, the most important gap or risk, one concrete suggestion>
```

## Structural gaps to look for
| Slice kind | Expect | Flag when missing |
|---|---|---|
| StateChange | an origin (screen action, form, or reaction), a `command`, at least one `event` | any of the three |
| StateView | a `readmodel` with a projection (or reducer) and a `query` or screen that reads it | projection or reader |
| Automation | a `reaction` or `trigger` and what it produces or invokes | either |
| Translate | inbound: a `capture` or a translating reaction, and the private events it produces; outbound: a reaction or an event-target projection/reducer publishing the public event contract (no capture) | the direction's source or the produced or published events |

Do not flag a gap the documentation or the slice name marks intentional (an internal slice
with no screen). Judge unintentional gaps against the slice title, and surface only those.
Orphans that a documented terminal reason explains are not findings.

## Not carried over
Slice status counts (`Planned`, `InProgress`, `Done`) are not in the report: `.play` has no
status field. Delivery status lives in the GitHub issue or `STATE.md`. An orphan that a board
tool could not see is skipped with a note, never guessed.
