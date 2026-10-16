---
name: cratis-screenplay-delivery-planning
description: "Plan and estimate the delivery of an accepted Screenplay model by slices: count slices by type (StateChange, StateView, Automation, Translate), derive the slice dependency table from the model, split work in parallel along event contracts, track slice groups as GitHub issues with a closed status vocabulary, and turn the team's own completed-slice history into ranged estimates, capacity math and retrospective metrics. Use when asked how long a modeled scope will take, how to split it across people or agents, what to build first, or how to track and re-plan delivery. Not for: deciding what to model (use `cratis-screenplay-event-modeling`), rendering or gap-filling a slice (use `cratis-screenplay-render-and-gap-fill`), or reviewing the model (use `cratis-screenplay-model-review`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-delivery-planning/SKILL.md -->

# Delivery planning

## Purpose
Turn an accepted `.play` model into a delivery plan: how many slices of which type, in which
order, who can work in parallel, what each group costs, and when it is done. The model already
cuts the work into slices and names the events between them, so the plan counts and orders
those slices instead of guessing at features.

Planning does not replace modeling, review or the render and gap-fill gates. A plan is a
working note, not a contract: when the model changes, recount; when a slice is not accepted,
it is not planned work yet.

## Ground rules
- **Count slices, not features.** The unit is one slice of one type
  (`StateChange`, `StateView`, `Automation`, `Translate`). Method and weights:
  `references/slice-counting.md`.
- **Estimates are ranges from the team's own history.** State the basis (which completed slices,
  how many, how long). With no history the estimate is a *first estimate without history*:
  say so, give a wide range, and replace it as soon as slices complete. Never quote an accuracy
  figure or a duration you cannot trace to a count and a rate.
- **Events are the contract.** Slice B depends on slice A because B needs an event or view A
  produces. Anyone can build B against the events A will produce, with synthetic `given`
  events, before A is finished. Dependencies are read from the model, not remembered.
- **Render is not free of effort, and gap-fill is not free of risk.** Stage renders
  `StateChange` and `StateView`; `Automation` and `Translate` are hand-written against the
  model as contract. Check admission per slice with `cratis-screenplay-render-and-gap-fill`
  (tool versions: `cratis-screenplay-toolchain` `SKILL.md`) before pricing a slice as
  rendered.
- **Plans live in `.ai-work/`; tracking lives in GitHub issues** where the repository works
  that way. Never put a plan in the model, in `.cratis/`, or in a documentation folder.

## Procedure
1. **Confirm the scope is accepted.** Only slices of a committed model under the root count
   as planned delivery. Drafts and open questions are listed separately as unplanned. If the
   model is still moving, plan only the stable modules and say what is excluded.
2. **Inventory the slices.** Use the Screenplay MCP: `describe-application` for counts and
   paged children, `dependency-graph` at slice level for edges, cycles and an advisory
   order, `dependencies` and `find-references` per slice for the contracts it consumes and
   produces and for the `invokes`, action and form edges the graph does not order (`reads` maps to `decidesFrom`, which it does order, but it stays a coordination point). Treat `complete: false` or an ambiguous binding as unknown, never as no
   dependency (`cratis-screenplay-model-authoring`). Without the MCP, read the `.play` files.
3. **Classify each slice** by type and by realization: rendered (Stage admits it) or
   hand-written (gap-fill, adapter, external call). Record the reason for each hand-written one.
4. **Build the dependency table** (`references/parallel-delivery.md`): per slice, the events
   it consumes, who produces each, and the slices it waits on. A cycle means a feedback loop:
   return it to slice design (usually an Automation slice); never schedule around it.
5. **Group into waves.** Slices with no unmet producers form wave 1; each next wave needs
   only earlier waves. Within a wave, slices are independent and can run in parallel. Group
   slices that share one view or one stream into one slice group (one issue).
6. **Weigh and estimate** (`references/velocity-and-capacity.md`): apply the team's weights per
   type and realization, divide by measured throughput, and report a range with its basis. Add
   separate lines for what is not slice work: modeling and review gates, integration with
   outside systems, environment and deployment, specifications that need new infrastructure.
7. **Track.** One GitHub issue per slice group, in the repository's own issue practice
   (assign the issue, keep an in-progress status in the issue body, check for an existing claim
   before starting): the status block lists the slices, their
   type, the wave, the blockers and each slice's planning status below. The issue's own state
   follows the repository's hygiene (in progress, in review when the PR opens, paused, blocked,
   done only when delivered). Keep the working plan in
   `.ai-work/<task>/delivery-plan.md`.
8. **Re-plan after each completed group.** Update throughput, recount if the model changed,
   restate the range. Record the retrospective metrics from `references/velocity-and-capacity.md`.

## Status vocabulary
Per-slice planning status, a closed set recorded inside the plan and the issue's status block.
It is not the issue's state: the enclosing issue keeps the repository's own states (for example
in progress, in review, paused, blocked, done), and is done only when its slices are
delivered (merged and released where the repository releases).

| Status | Meaning |
|---|---|
| todo | accepted in the model, not started |
| in progress | one owner is working on it |
| blocked | waiting on a named producer, decision or external party; say which |
| done | built, its specifications pass, and the slice conforms to the model |

Before claiming a slice, check its current status. If it is already `in progress`, someone else
holds it: pick another slice, do not take it over and do not report an error. Move a status only
to a different value; a repeated update is a signal that another worker got there first.
Check for an existing claim (assignee and in-progress status) before starting; where the harness
provides a claim tool such as `issue_work`, its claim step is that guard.

## What a plan states
- Scope: the modules and slices counted, and what was excluded and why.
- Slice counts by type, split into rendered and hand-written.
- The dependency table and the waves.
- The estimate as a range with its basis, or "first estimate without history".
- Capacity assumptions: who or what works in parallel, and the constraints on it.
- Risks that move the range: hand-written slices, outside systems, open questions, a model
  likely to change.

## Limits
- A rate from one project or one person does not transfer to another team or another kind of
  slice; say when the sample is small or mixed.
- Counting hides unusually large slices. A slice that needed a design discussion is two slices
  or a modeling question; recount rather than weigh it as "big".
- Parallel work only pays when slices are truly independent in the dependency table and
  integration of the shared events is owned. Shared writes to one file, one stream or one
  external system serialize the work.

## References
- `references/slice-counting.md` - types, realization, weights, what to count and exclude.
  Read when counting or classifying slices.
- `references/velocity-and-capacity.md` - measuring throughput, ranges, capacity math,
  retrospective metrics. Read when producing or updating an estimate.
- `references/parallel-delivery.md` - dependency table, waves, event contracts, issue
  grouping. Read when splitting work across people or agents.
- `references/provenance.md` - sources and what was not adopted.

## Related skills
`cratis-screenplay-slice-design` (slice dependencies), `cratis-screenplay-model-authoring`
(MCP navigation), `cratis-screenplay-render-and-gap-fill` (what renders, gap-fill handoff),
`cratis-screenplay-modeling-lifecycle` (phases and gates).
