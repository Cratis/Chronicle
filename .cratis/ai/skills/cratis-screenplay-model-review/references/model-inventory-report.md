<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/model-inventory-report.md -->
# Model inventory report

## Contents

- Gather (read-only)
- Report
- Information journey (optional)
- Structural gaps to look for
- Delivery status (optional)
- Not carried over
- Optional read-only passes

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
   Cost tiers: counts only need `describe-application` (view `summary`); gaps and shapes need
   declaration details and `dependency-graph`; do not read specifications when coverage was not
   asked.
2. Structure: `describe-application` (view `summary`) gives modules, features and slices by kind.
3. Elements: counts of `event`, `command`, `readmodel`, `screen`, `reaction` and `trigger`
   per module from `search-declarations`; fall back to the source files when a view is
   unavailable and say which.
4. Specifications: `find-assertion-gaps` for slices without an assertion; `find-specification-obligations`
   for obligations with no matching specification. These read authored examples, not executed
   coverage (`../../cratis-screenplay-model-authoring/references/mcp-tools.md`).
5. Gaps and orphans: `find-references` and `dependencies` for declarations referenced by nothing;
   `find-modeling-smells` for the candidate shapes; SMELL006 names event properties no
   mapping, reaction, condition or specification reads, a question to answer, not a defect.

## Report
Asked conversationally, give the summary and the most important gap and offer the full lists on request.

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
- n of total slices have at least one assertion (pct); split by slice kind (StateChange,
  StateView, Automation, Translate) when it helps
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

## Information journey (optional)

One piece of information per journey ("name and email" are two; do the first, offer the second).
An ambiguous subject: ask once, listing the candidate properties. Find the carriers by inspecting
the properties of the scoped declarations (`declaration-details`, `read-ast` or the model source):
`search-declarations` matches declaration names and addresses only, never properties, so an empty
name search does not mean there are no carriers. Then trace with `find-references` (`address`,
`kind: "Property"`) on each carrier's qualified property address, following the roles `mapping-source`,
`mapping-target`, `automap`, `screen-binding`, `assertion`, `key` and `condition` until nothing new
appears (model-authoring `references/property-links.md`). An incomplete result or `opaque` and
`unclassified` links are named as such, never guessed past. A property that only sounds related
(`emailVerified`) is different information.

| Role | The declaration ... |
|---|---|
| enters | first takes it in: a screen or form input, a capture, a translated event; the command when no screen is modeled. Several when it can enter several ways |
| recorded | an event that stores it (every such event is a stop) |
| changed | renames it (mapping), derives from it, or normalizes it as a specification or `documentation` states |
| used | a command that carries or checks it, a projection or read model, a screen that shows it, a specification that decides on it |
| leaves | a reaction or outbound translation that hands it outside |

Each declaration appears once, with the role that says most. Order by flow; branches follow their
source (after the recording event: its read models, then reactions). Open with a headline ("enters
in 1 place, recorded in 2 events, used in 3 places"), then the table (role, declaration address,
property name there, slice, note). A value recorded but never read, or shown but in no read
model, is a finding. For personal data, link the journey to the compliance processing record
(`cratis-chronicle-compliance`). Changes asked afterwards are ordinary model work.

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

## Delivery status (optional)
`.play` has no status field. When the brief names the tracking issues, count slices per delivery
status (todo, in progress, blocked, done; `cratis-screenplay-delivery-planning`) and flag stalled
work: `blocked` without a named cause, `in progress` with no change since the last period.
Otherwise the report carries no status counts.

## Not carried over
Slice status counts are not in the report without that brief. An orphan that a board
tool could not see is skipped with a note, never guessed.

## Optional read-only passes

Optional, on request, all read-only and yielding edit requests: one subject's information journey
(`references/model-inventory-report.md`), a resilience pass over business-critical scope after the
critic review and before acceptance (`references/stressor-analysis.md`), and a slice-by-slice
explanation or impact walkthrough (`references/walkthrough.md`).
