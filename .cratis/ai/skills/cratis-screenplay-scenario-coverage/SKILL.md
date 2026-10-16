---
name: cratis-screenplay-scenario-coverage
description: "Decide and write Screenplay specifications for slices: a coverage matrix before drafting, every applicable scenario type per command (rules, denial, duplicates and retries, competing claims, ordering, external failure, compensation, evolution), view lifecycle specs per read model, shared example data, spec forms per mode, and a workshop guide for scenario sessions. Use when writing or auditing given/when/then in a `.play` model. Not for: spec grammar and comparison semantics (use `cratis-screenplay-specifications`), C# specs (use `cratis-specifications-csharp`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/SKILL.md -->

# Screenplay scenario coverage

## Purpose
Turn designed slices into specifications that pin behaviour a business person would argue
about. Decide **which** situations need a specification before writing any (coverage matrix),
then write them with consistent example data in the form the chosen mode admits. Situations the
language cannot express today are recorded, not faked. Grammar and outcome comparison live in
`cratis-screenplay-specifications`; this skill owns coverage and method.

## When / when not
- Use: the scenario phase of `cratis-screenplay-modeling-lifecycle`; adding or changing a
  slice's specs; checking coverage of an existing model; deciding how to specify retries, races,
  cascades or event evolution; running a scenario workshop.
- Not for: grammar and comparison (`cratis-screenplay-specifications`); designing the commands
  or views (`cratis-screenplay-slice-design`); where invariants live
  (`cratis-screenplay-streams-and-consistency`); reaction and capture recipes
  (`cratis-screenplay-automations-and-translations`); C# specs (`cratis-specifications-csharp`,
  `cratis-specification-by-example`); a full audit (`cratis-screenplay-model-review`).

## Product sources
Screenplay `Documentation/screenplay/specifications.md` and `queries.md` and the standalone compiler
describe the released behavior (including ESM v7 fixtures and `then returns`). Reading the installed tool and bundle versions is covered in
`cratis-screenplay-toolchain` `SKILL.md`.
MCP `find-specification-obligations` derives SPEC001–SPEC008 from declarations;
met means matching authored presence, not full coverage or passing execution.
Use it to seed, never replace, the matrix. Multi-step storylines remain unavailable;
lifecycle families emulate them.

## Interview phase
**Skip if** the brief already states the coverage depth (rules, denials, state violations,
retries, races), the critical rules with an allowed and a refused example each, how the specs
will be used (reference only, executable, rendered and tested) and who validates them.
Critical questions, one at a time, with follow-ups:
Ask only what is missing: depth, known rules and edge cases (one allowed and the nearest refused request per rule), intended use (decides admitted forms), and who validates. Read [references/interview-batch.md](references/interview-batch.md) for the four questions and follow-ups.

**Unattended:** assume visibly, never invent a rule, and record who actually answered; an agent asking itself four kinds of question is not four approvals.

## Procedure
1. **Mode first** (`cratis-screenplay-modeling-lifecycle`): it decides the admitted spec forms.
   Fix the model root and source revision.
2. **Ask only what is missing** (Interview phase). Take discovery and slice-design outcome lists
   (Cannot lines, "only if", refusals, competition candidates, endings) as coverage seeds.
3. **Inventory before drafting**: read commands, effective gates (own, feature, module), rules,
   constraints, productions, reactions, captures, read models and queries in scope; derive the
   obligations from those declarations, not from specs already present. Use scoped
   `find-specification-obligations` and inspect unmet/message-specific obligations;
   ambiguous references and opaque code are not inferred coverage.
4. **Coverage matrix before any spec** (`references/coverage-matrix.md`). Rows: every command,
   reaction, capture, read model and gated query. Columns: the scenario types below. Each cell
   is `spec <Name>`, `n/a: <domain reason>`, `recorded: <why not expressible>`,
   `n/a: technical` (closed list), `open: <case>`, `question: <id>` or `gap: <code>`. "Fewer specs" is never a reason; a
   declaration (constraint, policy) or another command's denial is not a spec.
5. **Per read model, decide the shape**: single-transition specs only, or also a lifecycle
   family (cumulative givens). Write the decision and reason in the matrix, before drafting any
   payload: one line per read model. One schema reused for every command and view may indicate
   the per-view judgment was skipped: check for a recorded reason per view before concluding.
6. **Example data**: a small cast per feature; reuse only where meaning agrees; preserve
   existing intentional fixtures (`references/scenario-catalogue.md` "Example data"). Shared
   fixtures may live as typed `example`s in another document beside the declarations they type
   (`cratis-screenplay-specifications` `references/examples-and-routes.md`).
7. **Draft commands**: happy path, each rule, denial, duplicates and retries, competing claims,
   state violations, alternative branches, compensation, external failure, evolution. In each
   rejection vary only the value under test. Use typed `parameter`/named `case`
   tables for a shared validation matrix, not for unrelated behaviors; rows are
   cases, not typed fixture `example`s. `given caller as <Persona>` supplies one
   authenticated policy witness for success/denial; use explicit callers for
   anonymous/ownership or unsynthesizable personas. Syntax and complete model:
   `cratis-screenplay-specifications` `references/persona-callers-and-cases.md`.
8. **Separate view pass**: population, each update, accumulation, removal (with a surviving
   row), absence, ordering. Mark lifecycle branches. Views combining sources must
   tolerate any arrival order. Pin a child before its parent (Requires Screenplay 4.149.1 or later):
   given child, then parent, then read model; "parent never arrives" leaves the
   child absent. Evidence/example: `references/view-and-story-specs.md`.
9. **State-dependent rules**: `reads <View>` + `require <expr> message "..."` as stated intent,
   marked **NOT enforced in the model today** in the slice `description` (binding reports
   PLAY0268/0271: expected in design mode; Screenplay#129), target named (Arc
   `[ProtectedDecision]` + `DecisionRead<T>`, Chronicle DCB, or a constraint). Forbidden:
   caller-supplied copies of state in `require`, boolean attestation inputs (`confirmsX ==
   true`) and rules hidden in `handler`/hint prose. Never drop an unresolved given or assertion
   to make a spec compile: record the blocked case and continue.
10. **Validate** the batch (V1, warnings as errors); V2/V3 when executable or renderable. Fix diagnostics without dropping intended rules, prerequisites or assertions, then rerun the applicable verdicts; if the chosen mode cannot admit the construct, preserve it and report the blocked scope explicitly. The
    phase report carries the matrix path, spec names added or changed, `recorded`, `open` and
    `gap` cells and open questions.

## Scenario types (decide each per command; detail in `references/scenario-catalogue.md`)
Decide each of these per command: happy path; rule rejection (one spec per rule, exact pinned `then error`); denial (`given caller` without the policy, `then denied`, one per gated command and query); state violation; duplicate or retry; competing claim; alternative path; ordering; external failure; compensation; evolution; and the view pass. Read [references/scenario-catalogue.md](references/scenario-catalogue.md) ("Scenario types at a glance") for each type's question and usual shape.

For recovery, `when redelivered <Event> to <Reaction>` must select one definitely
matching effective given and no undecided candidate. It delivers only to the named
reaction without re-appending the input; `then no events` explicitly checks no
new facts from the settled cascade (ESM v17). Read `references/scenario-catalogue.md`
("Redelivery selectors") when choosing retry/no-effect cases or `stream`/`no stream`.
Read [query rejection examples](../cratis-screenplay-specifications/references/query-rejections.md)
when specifying malformed keys/filters versus valid absence (ESM v18).
Requires Screenplay 4.157.0 (`screenplay --version`) for v17, 4.161.0 for query
validation/error assertions; older tools do not execute these contracts.

**Once, idempotent, exactly once are different claims.** A `unique event` constraint proves the
*fact* occurs once per event source; it does not make *command handling* idempotent (a retry is
rejected, not answered with success), and nothing in the model proves an *external effect* (an
email, a payment) happens exactly once. Say which of the three a spec proves; record the rest as
target requirements.

## Rules
### Compiler contracts
- At most one `when` action; with none, assert only read-model or query outcomes.
- `then` events are exact in count and order (`then events in any order` relaxes order only).
- Command denial: `then denied` with no success or error outcome. Read-only query denial:
  `then query <Query>` with `arguments` and no `result`, then `then denied`. Every gated command
  or query spec needs `given caller` or `given caller as <Persona>` (module and feature gates count); an authorized scenario
  gets no inferred fixture (PLAY0389). Unauthorized is never `then error`.
- Duplicate and competing specs use `when <Command>`; `when append` is for projection and
  automation triggers, and for a constraint the command's own validation would reject first.
- `null` only for optional read-model properties; constraint messages are copied verbatim.
- Executable-only binding checks (PLAY0350/0352/0388/0389/0273) do not show in V1: run V3.

### Modeling defaults
- Every slice has at least one specification at the application boundary.
- One behaviour per spec; names read as sentences: `Reserving...`, `Rejecting...`, `Refusing...`
  (denial), `<View>LifecycleN<State>` for families.
- `for` on every given, appended and expected event (plain `produces <Name>` too; inline `produces event` omits it), and on the `when` command where the
  destination matters. A competing-claim given needs a *different* `for`.
- Format rules belong on a `concept`: give each concept rule representative acceptance,
  rejection and boundary specs once (through one command that carries it), not on every use;
  never `n/a` automatically. Include a whitespace-only rejection for text `not empty`:
  this changes outcomes in every ESM version (Requires Screenplay 4.151.0 or later). Unicode/identity boundaries
  and evidence: command-surface `references/validate-rules.md`.
  A business rule gets one rejection spec if an expert could recognize it from its message;
  binder-enforced shape is no scenario (catalogue §2).
- Persona Cannot lines are intent: each needs an executable gate plus a `then denied` spec whose
  caller is a synthesized persona witness or an explicit gate fixture, else record a gap.
- Descriptions are not rendered: a rule only in prose is unenforced; make it a rule layer, a
  spec or a recorded target requirement.
- Prove projections with events (`given`/`when append`), not `given readmodel`; seed read models
  only for performer-backed views or commands that decide on view state.
- No contradiction between a family and single-transition specs of the same view.

### Quality checklist (adapted from the TrogonStack scenario checklist; `references/provenance.md`)
Read [references/quality-checklist.md](references/quality-checklist.md) before reporting a scope spec-complete. No command has only two specs (success and one rejection) unless every other type was reviewed and found inapplicable; a model with dozens of command specs and no read-model spec is not scenario-complete.

### Review questions
Read [references/review-questions.md](references/review-questions.md) for the race, retry, arrival-order, correction and cross-context questions.

## Public-event coverage (ESM v9)

Outbound publication needs `when append <private>` and `then <PublicEvent>`. Read [references/public-event-coverage.md](references/public-event-coverage.md) when specifying public events or routed producers and observers.

## Version skew: reaction cascades and false PLAY0285
Under ESM v6 a command spec's `then` lists the events reactions cascade from it; an older `cratis` bundle reports a **false PLAY0285** for it. Never delete the reaction. Read [references/version-skew.md](references/version-skew.md) when a spec with cascade events reports PLAY0285 or the bundled compiler is older than the standalone tool.

## Gate
A scope is spec-complete only when: the matrix has no `open`, `question` or `gap` cell (or the
scope is reported as explicitly incomplete); V1 passes with the tool named in
`cratis-screenplay-toolchain`; for executable or renderable scope V3 is binding-ready.

## Verify
- Re-inventory declarations at the reported revision: every applicable obligation maps to an
  existing spec whose givens, action and assertions exercise it.
- Commands and read models are checked in separate passes; each removing projection has a
  populated-then-removed spec.
- V1 passes (standalone `screenplay` required when cascades are specified); report tool and
  version.
- Run V4 through standalone `screenplay test` or MCP `run-specifications`;
  propose → review → run with `proposalId` →
  apply (Requires Screenplay 4.145.0 or later). PLAY0285 (impossible `then`) unbinds
  the whole model; no spec runs.
  if not executed, report the actual host/input reason, not a blanket no-route
  claim. Stage's runner is target-engine evidence, never V4; V1/V3 do not prove pass. V5 is "not run" unless rendering was attempted; report admission, publication, build and
  tests separately.
- Match guarantees to evidence: `.play` outcomes, in-process Chronicle scenarios and real-kernel
  tests are not interchangeable (`references/chronicle-verification.md`).
- Reference example `references/berth-reservations.md`: compiles with warnings as errors.

## Route near misses
Read [references/neighbouring-skills.md](references/neighbouring-skills.md) when the work is grammar, rules, projections, reactions, invariant placement, review or C# specs.

## References (load on demand)
- `references/scenario-catalogue.md` - each type: ask, applies when, n/a when, shape, traps; example data. Read when deciding applicability, eliciting cases or choosing example data.
- `references/view-and-story-specs.md` - view specs, lifecycle families, ordering, lag. Read before deciding each view's lifecycle family and drafting view specifications.
- `references/coverage-matrix.md` - obligations from declarations, template, worked matrix. Read before constructing or reconciling the coverage matrix.
- `references/scenario-examples.md` - compact worked examples per category, good and bad forms,
  list and todo-list views; copied from the complete `references/invoicing-dues-example.md`
  (runnable) and, for compensation, `references/invoicing-dues-design.md` (design mode: holds the
  stored-state rules, does not bind). Read `references/scenario-examples.md` when drafting a category and you need good/bad or worked specification forms; read `references/invoicing-dues-example.md` when you need the complete executable declarations behind the invoicing excerpts; read `references/invoicing-dues-design.md` when specifying stored-state invoicing rules or their compensation scenarios in design mode.
- `references/scenario-workshop.md` - facilitation guide for group scenario sessions. Read when facilitating a group scenario session.
- `references/chronicle-verification.md` - guarantees that need more than a `.play` outcome. Read when a claimed guarantee needs in-process or real-kernel evidence beyond `.play` outcomes.
- `references/spec-forms-by-mode.md` - admitted forms in design, executable, renderable. Read after choosing the mode and before drafting its admitted specification forms.
- `references/berth-reservations.md` - complete compiled example (marina guest berths). Read when you need a complete design-mode example of a coverage matrix realized as specifications.

## Lineage
Attribution and adapted sources: `references/provenance.md`. Read `references/provenance.md` when checking attribution or how an adapted practice differs from its source.
