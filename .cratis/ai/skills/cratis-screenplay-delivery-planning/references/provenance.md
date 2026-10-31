<!-- cratis-ai-managed: skills/cratis-screenplay-delivery-planning/references/provenance.md -->
# Provenance

## Method lineage
The planning method (count the modeled units, let explicit contracts allow parallel work, estimate from the team's measured throughput, plan capacity and review with retrospective metrics) comes from the Event Modeling community: Adam Dymitruk's Event Modeling, and Martin Dilger's writing on estimating from event models. They are cited as practice. The text of this skill is written for Screenplay slices and Cratis delivery.

## Adapted closely (Martin Dilger and Nebulit GmbH)

Source: https://github.com/Nebulit-GmbH/agentic-engineer at commit `07b0f30648d663cb588d7e2c7aa031af9dfc21f2`,
by Martin Dilger and Nebulit GmbH (https://nebulit.de), used with the explicit consent of Martin Dilger and Nebulit GmbH.

| Source file | Used in | How |
|---|---|---|
| `.claude/skills/eventmodeling-orchestrating-event-modeling/references/project-planning-with-event-modeling.md`: contracts let steps proceed in parallel | `SKILL.md` ground rules, `references/parallel-delivery.md` | adapted closely; steps become slices, contracts are the events and views a slice consumes and produces in the `.play` model |
| same: velocity measured from history, estimating a project by counting | `SKILL.md` procedure, `references/velocity-and-capacity.md` | adapted closely; velocity becomes throughput of weighted slices from issue history, results are ranges with a stated basis |
| same: variable complexity (typical, complex, simple steps) | `references/slice-counting.md` "Weights" | adapted; weights are by slice type and realization and are calibrated by the team |
| same: capacity planning, dependency handling across teams, organization-level velocity | `references/velocity-and-capacity.md` "Capacity", `references/parallel-delivery.md` "Waves" | adapted; waves derived from the model's dependencies instead of a spreadsheet |
| same: common estimation mistakes (equal weights, non-implementation work, ignoring dependencies) | `references/slice-counting.md` "What not to count", `references/parallel-delivery.md` | adapted closely |
| same: retrospectives and key metrics table | `references/velocity-and-capacity.md` "Retrospective metrics" | adapted closely; metrics extended with cycle time by class, blocked time and rework |
| `.claude/skills/update-slice-status/SKILL.md`: closed slice status vocabulary and the already-in-status guard | `SKILL.md` "Status vocabulary" | adapted; four statuses (todo, in progress, blocked, done) applied to GitHub issues, the board API dropped; the guard becomes "pick another slice" and `issue_work` claim |

## Not adopted
| Source material | Why |
|---|---|
| "±50% versus ±10%" accuracy claims and "4-6 months guess, 6-9 months actual" comparisons | no source or data given; Cratis estimates are ranges derived from the team's own counts and rates |
| "Flat cost curve" diagrams and fixed example numbers (3 steps per sprint, 1 week per step) | illustrative figures, not facts; a team's throughput is measured, not assumed |
| Sprint and week-based schedule spreadsheets | replaced by waves, slice groups and ranges |
| The eventmodelers board, token and slice-status API, MCP slice listing, and node events | Screenplay MCP navigation and GitHub issues replace them |
| Statuses Created, Planned, Review, Assigned, Informational | replaced by the four-status set; review is a gate in the lifecycle, assignment is the issue assignee |
| Autonomous continue-without-asking behavior in the upstream orchestration | Cratis keeps the model-first gates and user authority; planning never skips review or acceptance |
