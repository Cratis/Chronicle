<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/phases.md -->
# Phases: Input, Carry-forward, Gate

## Contents

- Phase transition protocol
- P0 Intake
- P1 Discover (`cratis-screenplay-discovery`; modeling stance)
- P2 Model (`cratis-screenplay-slice-design`, `-streams-and-consistency`, `-automations-and-translations`)
- P3 Specify (`cratis-screenplay-scenario-coverage`; modeling stance)
- P4 Self-check (`cratis-screenplay-model-review`, self-check; critic stance)
- P5 Independent review (`cratis-screenplay-model-review`)
- P6 Accept (the user)
- P7 Execute (executable mode; modeling stance)
- P8 Render or fall back (`cratis-screenplay-render-and-gap-fill`)
- P9 Verify
- Entry points for changes to an existing model
- Resume mid-workflow
- Final output of a modeling engagement
- Quality checklist (closing)
- Legacy entry (replaces P1-P2)

Every phase uses the same template, adapted from the per-step "Input / Output to carry forward /
Gate" structure of the TrogonStack orchestrating skill (see `provenance.md`). Owners are roles:
the **main session** orchestrates and owns the MCP connection while it is the identity owner;
the **Screenplay Modeler** agent models and extracts legacy systems; the **Screenplay Reviewer**
(read-only) reviews and explains; the **Screenplay Renderer** probes and verifies delivery.
Where a harness has no such agents, the main session plays the roles one at a time, never mixing
modeling and critic stance in one pass. Subagents do not spawn subagents. One authoring agent per
model root at a time; parallel work is read-only or on separate roots.

## Phase transition protocol
Before loading the next phase skill, overwrite STATE.md (`handoff-template.md`):
1. what was done (changed declarations, gate result, verdict lines at the current source identity);
2. carry-forward for the next phase (the items below);
3. open questions with address and assumption in use;
4. the next phase and the skill it loads.
Add one Interview Trail row (phase, skill, status, key output). Then load the next skill. A phase
whose gate fails stays open; it is never "carried" into the next one.

Phase summary block, appended under Carry-forward in STATE.md after every phase and before the
next skill loads (never skipped, also after a small change):
```markdown
### P<n> complete - <skill>
- **What was done**: 2-4 bullets: artifacts changed (addresses), decisions made, gates passed
- **Carry-forward**: what the next phase needs from this one
- **Open questions**: anything unresolved or deferred, each with its address and assumption
```
The Interview Trail row's status becomes `done` and its key output one line. Also record, at any
phase, a decision important enough that a later reader could misread the model (see
`reasoning-notes.md` for when it belongs in fenced Markdown `documentation`).

## P0 Intake
- **Input**: the request, the repository, the project's model folder or the configured root.
- **Carry-forward**: mode; model root; tool check (versions per `cratis-screenplay-toolchain`);
  the decision-rule outcome (model or code-first); STATE.md.
- **Gate**: mode stated; STATE.md exists; the five intake points (domain, source, goal, constraints,
  starting point) settled or recorded as assumptions.

## P1 Discover (`cratis-screenplay-discovery`; modeling stance)
- **Input**: the user, documents, existing `.play`.
- **Carry-forward**: personas with purpose; modules and features; past-tense events in story order
  with terminal states; rules register and denial candidates; open questions.
- **Gate**: every in-scope workflow has events in a feature ending in named terminal states; every
  persona has a purpose; rules register and denial candidates listed; naming pass done (no generic
  edit events); open questions logged.
- **Skip when**: an existing model already covers the workflow (change entry).

## P2 Model (`cratis-screenplay-slice-design`, `-streams-and-consistency`, `-automations-and-translations`)
- **Input**: the P1 model.
- **Carry-forward**: slices with commands (reachable, authorized origins), events, read models with
  projections and queries, screens, reactions and captures, consistency decisions, slice dependencies.
- **Gate**: V1 clean after the batch; every command has a reachable, authorized origin; every screen
  data need is served by a read model; every field has a known origin; every persona's Does line
  reaches a screen or form `action` for that decision and every Reads line has a screen whose
  `data` shows it. A persona with neither is missing screens (add them) or is not part of this
  scope (revise its Does/Reads in discovery, or record why). A read-only persona is legitimate.
  The opt-in `personas` check (`PLAY0574`, `PLAY0575`) proves gate reachability, not screen
  reachability, so this walk stays manual.
- **Skip when**: no reactions, schedules or outside facts are in scope (automations); event-source
  identity, destinations, constraints, stored-state decisions, retries and persisted-contract
  evolution are absent or already verified and unchanged (streams). Command count is not the criterion.

## P3 Specify (`cratis-screenplay-scenario-coverage`; modeling stance)
- **Input**: the P2 model.
- **Carry-forward**: coverage matrix, specifications, then reasoning documentation, written last in P3, for features with
  real decisions (`reasoning-notes.md`).
- **Gate**: matrix written before drafting; per command each applicable scenario type is a spec or
  "n/a" with a domain reason (one success plus one failure is not coverage); each gated command and
  query, including those under an inherited module or feature `authorize`, has its own `then denied`
  spec with a synthesized persona caller or an explicit effective-gate fixture; each `produces when` has a spec
  per branch; each value-unique constraint has a competing-claim spec; per read model a view spec, an
  update spec when several sources feed it, a removal or absence spec when it removes. A command-only
  pass fails the gate.

## P4 Self-check (`cratis-screenplay-model-review`, self-check; critic stance)
- **Input**: the P3 model including reasoning documentation.
- **Carry-forward**: five verdict lines from fresh runs; the completeness walk; findings by tier.
- **Gate**: completeness walk done (field origins, event consumers or terminal reasons); every gap
  fixed, accepted by the user, or an open question with its assumption. The critic reports; fixes go
  in a separate modeling batch, then V1 is rerun on the changed revision.

## P5 Independent review (`cratis-screenplay-model-review`)
- **Input**: the handoff packet, the source identity, every author's model (or `model: not exposed`).
- **Carry-forward**: findings (critical, major, minor) with tier; PASS, WARN or FAIL; business
  questions; the reviewer's own model.
- **Gate**: no critical finding; majors fixed or accepted by the user; at most 2 fix rounds. A
  same-model review is labelled as such; the user decides whether it is enough.

## P6 Accept (the user)
- **Input**: the reviewed source identity.
- **Carry-forward**: the accepted **source identity** (`verdicts-and-modes.md`).
- **Commit at P6**: the accepted `.play` files and `.screenplay/identities.json` when present.
  The user commits, or explicitly authorizes the owning session to commit; the modeler never
  commits on its own. Run `cratis-screenplay-modeling-lifecycle`'s `scripts/model-first-status.mjs` to check acceptance, drafts and model changes in progress.
- **Gate**: explicit acceptance and the model commit before P7-P9. After committing, recompute
  the source identity. Its digest must equal the digest reviewed at P5; only the commit part may
  differ. If the digest differs, return to P4/P5 for the changed scope. Record the post-commit
  identity only after this check passes. Any later edit to the scope invalidates that identity's acceptance; it
  does not replace the HEAD contract until committed.

## P7 Execute (executable mode; modeling stance)
- **Input**: the accepted identity.
- **Carry-forward**: V3, and V4 where a route exists.
- **Gate**: V3 ready; V4 result or "V4 not run: no route". Specs written or bound is never "specs pass".
- **Optional after V3**: a behavior walk in the Stage sandbox (`cratis-stage-rendering-and-sandbox`); findings go to the model owner, and it is not V3, V4 or V5 evidence.
- **Skip when**: design mode.

## P8 Render or fall back (`cratis-screenplay-render-and-gap-fill`)
- **Input**: the accepted identity.
- **Carry-forward**: the admission result; render output and named gaps in `Customizations/`;
  non-renderable scope handed to a code agent with the model as contract, plus a fallback record
  naming the blocking codes. Stage 4.89.1 renders supported event-triggered Automation; Translate/captures and refused reactions remain gap-fill (mixed effects keep one modeled reaction; `STAGE-ESM-037` is a redelivery-safety refusal, not a reason to split it). Three cases stay distinct: admission failure (nothing rendered); an
  existing generated base plus separately authorized gap-fill; fully hand-written delivery.
- **Gate**: V5 sub-results, or a fallback entry per non-renderable scope. A customization never
  makes a rejected model renderable; no whole-application V5 from a subset.
- **Skip when**: no code is wanted.

## P9 Verify
- **Input**: rendered or hand-written output.
- **Carry-forward**: Debug build and tests mapped to spec classes; drift check; for gap-fill, the
  conformance result (`cratis-application-slice-conformance`).
- **Gate**: all gates green or honestly reported as not run, with the reason.
- **Drift**: compare successive render manifests (`semanticRevision`) produced with the same
  inputs, `--name` and toolchain; never the MCP `modelRevision` against a manifest
  (`verdicts-and-modes.md`). Equal revisions do not prove customization conformance or faithful UI.

No other phase is skipped. P3-P5 scale to the change but always run for semantic edits.

## Entry points for changes to an existing model
| Change | Enter at | Then |
|---|---|---|
| New or changed slice, member, rule, read model | P2 | P3 for affected specs, P4, scoped P5 |
| Specs only | P3 | P4, scoped P5 |
| Rename, move, remove, contract evolution | P2 by the identity owner | typed proposal, diff, apply (the request that named the change is the approval); P3-P5 for impact |
| Review request | P5 | report only |
| Executable or render request on an accepted model | P7 / P8 | P9 |
| Question from a running Chronicle system | read-only `cratis-chronicle-cli-operations` | update the model at P2 if needed |

**Slices under delivery.** Where a delivery plan exists (the `cratis-screenplay-delivery-planning`
status block), check a slice's planning status before changing it. A `todo` slice changes through
the entry points above. A slice that is `in progress`, `blocked` or `done` is read-only for
modeling unless the request names it. Naming it is the approval for exactly those declarations
and nothing wider. Otherwise do the rest of the request, return an edit request naming the slice,
its status and the intended change, and open the packet's report with that part as NOT done. The
reason: someone is building against the slice as it stands. Changing a `done` slice reopens its
delivery: tell the plan owner, and expect conformance (`cratis-application-slice-conformance`) and
the affected specifications to rerun.

## Resume mid-workflow
1. Read STATE.md only, not old transcripts.
2. Recompute the source identity and compare. If it moved, rerun V1 and re-orient before trusting
   recorded verdicts.
3. Continue at the recorded next phase; carry open questions and assumptions forward.

## Final output of a modeling engagement
Personas with purpose; modules and features; events in story order; commands with origins; read
models and screens; consistency decisions; automations and translations; specifications with the
coverage matrix; open questions with assumptions; the five verdict lines; the review result; the
accepted source identity. A design-mode engagement lists what a later executable or renderable
mode would still need.

## Quality checklist (closing)
- [ ] Every phase either ran its gate or recorded its skip condition.
- [ ] Every persona has a purpose and every Cannot line resolves to an executable gate and a `then denied` spec, or is recorded as a gap.
- [ ] Every command has a reachable, authorized origin and at least one refusal where it can refuse.
- [ ] Every read model serves a screen or a processor; every field has an origin and a destination.
- [ ] Every decided rejection is a specification; every open question has an address and an assumption.
- [ ] No generic edit events; no rule exists only in a `description`.
- [ ] Five verdict lines, each a result or "not run: <reason>".
- [ ] Independent review done or labelled; acceptance bound to a source identity.
- [ ] STATE.md current; the packet opens with `Outcome:`.
- [ ] Nothing weakened to pass a tool; gaps recorded.

## Legacy entry (replaces P1-P2)
Owner: Screenplay Modeler with `cratis-screenplay-legacy-extraction`; a read-only exploration agent
for code sweeps; `cratis-chronicle-cli-operations` for Chronicle runtime facts only.
- **L0 Inventory**: stacks, entry points, data stores, omitted entry points.
- **L1 Static evidence**: `cratis screenplay generate` into `.ai-work/screenplay/<model-slug>/legacy/`.
- **L2 Dynamic evidence**: Prologue capture; approval naming the target.
- **L3 Evidence table**: each row intent, consequence, infrastructure or unknown, with file:line or
  capture reference. Absence of observation is not absence of behavior.
- **L4 Intentional model**: re-sliced by business decision; as-is evidence kept apart from target
  decisions (keep, change, remove); loss report; expert questions.
Then P3 (every command gets a success spec and a denial spec where gated), P4, P5, expert
verification with the user, P6. Recovered code and observations are evidence, never automatic truth.
