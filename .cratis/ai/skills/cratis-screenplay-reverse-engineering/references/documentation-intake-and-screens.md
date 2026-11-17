<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/documentation-intake-and-screens.md -->
# Documentation-only intake and product-like screens

## Contents

- Documentation-only intake
- Assumptions
- Fidelity without ground truth
- Deriving product-like screens
- Running constraints to plan around
- Verify

Use when the only sources are a product website, user documentation or a single description
document (no running app, HAR or executable), and when the run must end in a Stage app that feels
like a product, not a list of command forms. The platform limits in the last section are current
limits to check against the installed toolchain version, not permanent facts.

## Documentation-only intake

- Record every page or section as a source with a locator: URL plus section heading or anchor
  (for example `https://example.test/features#tasks`). Evidence strength is `Stated`: the
  documentation says it, nothing was observed.
- On a large docs site, look first for an `llms.txt` index or a Markdown form of each page, pick
  the pages per in-scope object and record each as a source id.
- Docs describe features, not rules. Do not infer server behavior, uniqueness, authorization or
  limits from marketing text; classify them `unknown` until stated.
- Prefer the smallest usable behavior: the few commands that make each main entity creatable,
  changeable and listable. Leave breadth (sharing, reminders, integrations, imports) for later.
- Write the out-of-scope list at intake with [scoping.md](scoping.md): each documented feature
  not modeled, with its source locator, so nothing disappears silently.

## Assumptions

A model needs rules, fields and ranges the documentation never states. Each one is an
assumption, never a fact:

- Mark it in the model: `// assumed: <why>`, for example
  `concept Priority : Int  // assumed: 0 to 5; the docs name only the two ends`.
- Add a row to `assumptions.md` in the work area: id, model locator, the assumption, why it was
  needed, the source that came closest, and the question that would settle it.
- Choose the least restrictive value that still lets the feature work; a wrong strict rule
  blocks users, a wrong loose one only misses a check.
- A description that states no rules makes almost every rule `assumed`: say so in
  `event-model.md` with the count of assumed against sourced rules.

## Fidelity without ground truth

With no running system or specification to compare against, the fidelity check becomes a
**plausibility report**: for each documented journey, does the running app let a user do it, and
which assumed rules did it depend on. Never score it as a match percentage. When a public API
description or documented behavior exists (an OpenAPI document, an API reference), score against
it as in [fidelity-check.md](fidelity-check.md), and say that the public description, not the
live system, is the ground truth.

## Deriving product-like screens

A model of commands and projections renders as a pile of forms unless the navigation is derived
deliberately. Derive these from the documented user journeys:

1. **A `layout` with a sidebar** holding the few workspace or list screens, one per main entity
   (for example Projects, Tasks, Labels in a to-do app), and a `ui profile` rooted on that
   layout. Take the exact syntax from `cratis-screenplay-ui-composition`; invalid syntax fails
   validation and is not a reason to drop the layout.
2. **Commands are actions on those list screens**, not navigation entries. Give each list screen
   `action <Command> for screen` (create) and `for item` (per row) with a readable label. A
   command screen exists only as the target of such an action.
3. **Display names through projection joins**, never raw ids. A task list shows the project
   title: `join projectTitle on projectId` fed by the events that set and rename the title.
4. **A detail or filtered screen per parent-to-children path**: the parent table has
   `on row-click navigate to <Children> by <parentId>` and the child screen reads the query
   filtered by that id.
5. **Avoid binding a table column to a child collection** (labels, assignees) until the Stage
   renderer formats them; it can show `[object Object]`. This is a known Stage renderer gap:
   keep the collection in the read model, show it on a detail screen or leave the column out,
   and record the gap.
6. Check a type before using it on an input: Stage may reject some (for example date-time). Use
   a date when the documentation does not evidence a time of day.
7. A staged pipeline is a table with a move action unless a Stage run of
   `component Cratis.Blueprint.Default.kanbanBoard` is proven.

## Running constraints to plan around

Check each against the installed `cratis` version before relying on it:

- **Queries written as code (opaque queries) are not served by `cratis run`.** Express each
  query as a projection-backed read model the run can serve; keep an opaque query only as a
  recorded boundary.
- **Authenticated paths need sandbox identities.** Check whether the installed Stage forwards an
  authenticated identity and enforces policies; where it does, declare sandbox users for the
  personas and walk each guarded command as them. Where it does not, keep the policy in the model
  (never weaken it to make the run pass), record a platform gap, and smoke-test the
  unauthenticated paths. A sandbox pass proves nothing about production security.
- **Layout and profile support in Stage may be reported as deferred** by a diagnostic. Keep them
  in the model when they validate; they still describe the intended product.

- **Run proof beats validation.** Constructs that validate and pass `screenplay test` may still
  be refused or ignored by Stage (clock rules, application triggers, pickers fed by a query,
  forms with only labels). Follow "Runnability" in [fidelity-check.md](fidelity-check.md): the
  complete model stays the deliverable, and a copy without the refused construct is only for the
  run and the walk.
- **Record each platform defect you work around**, with the workaround, as an issue draft for the
  owning repository. Filing it is an external effect: file only when the request covers it, and
  as a subagent return the draft to your parent. Do not bake the workaround into the model as if
  it were intent.

## Verify

- Look at the running app, not only the endpoints: take a screenshot of each sidebar screen, one
  row-click navigation and one create action. A 200 on a query proves data comes back, not that
  the UI is usable. Without browser tools, report "UI unverified: needs a visual check".
- Name each recorded platform gap and each assumption row in the report.
