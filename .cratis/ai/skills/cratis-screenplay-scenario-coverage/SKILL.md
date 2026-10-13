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

## Verified product sources
Screenplay v4.66.0 (`c89198b`) and v4.68.0 (`79801bf`, ESM v7 fixtures and `then returns`) were the
original sources; released behavior was re-read at v4.139.0 (`Documentation/screenplay/specifications.md`,
`queries.md`, the standalone compiler). Current pins, the bundle and the probes are in
`cratis-screenplay-toolchain` `references/versions.md`. At Screenplay v4.127.0,
MCP `find-specification-obligations` derives SPEC001–SPEC008 from declarations;
met means matching authored presence, not full coverage or passing execution.
Use it to seed, never replace, the matrix. Multi-step storylines remain unavailable;
lifecycle families emulate them.

## Interview phase
**Skip if** the brief already states the coverage depth (rules, denials, state violations,
retries, races), the critical rules with an allowed and a refused example each, how the specs
will be used (reference only, executable, rendered and tested) and who validates them.
Critical questions, one at a time, with follow-ups:
1. **Depth.** Reference examples only, every command variation, or production-grade including
   edge cases? Production work needs the full matrix. Follow-up: which commands matter most?
2. **Known rules and edge cases.** Which mistakes would be costly? For each rule: one allowed
   request, the nearest refused one, and why they differ (`references/scenario-catalogue.md`
   "Eliciting cases").
3. **Use.** Design record, executable (V2/V3), reference run (V4) or rendered tests (V5)? It
   decides the admitted forms (`references/spec-forms-by-mode.md`).
4. **Validation.** Who confirms the business outcomes: domain expert, developer, tester? For a
   group session use `references/scenario-workshop.md`.
**Unattended:** assume visibly. Write each assumption as a `question` cell with the default you
used; never invent a rule. Record who actually answered; an agent asking itself four kinds of
question is not four approvals.

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
   `open: <case>`, `question: <id>` or `gap: <code>`. "Fewer specs" is never a reason; a
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
   row), absence, ordering. Mark lifecycle branches as branches.
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
| Type | Question | Usual Screenplay shape |
|---|---|---|
| Happy path | What facts does a valid request establish? | `when Cmd` -> every firing production, payload exact |
| Rule rejection | Which input is refused, with what message? | one spec per rule; `then error "<exact message>"` (prefer `$strings.<key>`); always pinned (a bare `then error` is valid language but not accepted here) |
| Denial | Who must not do this? | `given caller` without the policy -> `then denied`; one per gated command and query, only where the whole Boolean gate is false; ownership claims need other-owner denial |
| State violation | Which prior facts make this wrong? | givens + constraint (`unique event` exclusivity) -> `then error`; else `recorded` |
| Duplicate / retry | Same request again (lost acknowledgement)? | `when <Command>`, same identifier, earlier fact as `given` -> constraint message, or the accepted repeat |
| Competing claim | Two people claim the same thing at once? | `when <Command>`, other source's fact as `given` (different `for`) -> constraint "another event source..." |
| Alternative path | Which branches exist, including "nothing happens"? | true and false witnesses per `produces when`; unconditional productions always fire |
| Ordering | Facts late or out of order? | view family with reordered givens; translation specs; else `recorded` |
| External failure | The other system refuses, times out or answers twice? | outcome as a recorded fact (capture/translation), never a fake rejection |
| Compensation | How is this undone, what does undoing free? | reversal command + spec showing the released claim or removed row |
| Evolution | Do old facts still mean the same after a contract change? | spec on the new generation; compatibility reasoning in `documentation` |
| View | What does each screen show after each fact? | `given` events (`for`) -> `then query`; `when append` -> update/removal |

For recovery intent, `when redelivered <Event> to <Reaction>` must select one
definitely matching effective given and no undecided candidate. Name `stream`
(with a complete id) or `no stream` when givens differ only by route; omitting it
is a wildcard, not an unrouted selector. Redelivery remains authoring-only
(PLAY0268), never evidence of an executed retry. Details:
`cratis-screenplay-specifications` `references/examples-and-routes.md`.

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
  never `n/a` automatically. This matches `cratis-screenplay-specifications`, which also
  requires one rejection spec per concept rule. Business rules get one rejection spec each.
- Persona Cannot lines are intent: each needs an executable gate plus a `then denied` spec whose
  caller is a synthesized persona witness or an explicit gate fixture, else record a gap.
- Descriptions are not rendered: a rule only in prose is unenforced in rendered scope; turn it
  into a rule layer, a spec or a recorded target requirement.
- Prove projections with events (`given`/`when append`), not `given readmodel`; seed read models
  only for performer-backed views or commands that decide on view state.
- No contradiction between a family and single-transition specs of the same view.

### Quality checklist (adapted from the TrogonStack scenario checklist; `references/provenance.md`)
- Every command has a success spec, a spec per rule, and state-violation specs where a lifecycle
  exists; every gated command and query has its own denial.
- Givens state the facts and their sources; outcomes list the exact events or the exact message.
- Every read model has population, update and (where it removes) removal specs.
- Alternative paths, compensation and external failure are specified or carry a reason.
- Each matrix cell holds a value; each `n/a` is a domain reason.
- No command has only two specs (success and one rejection) unless every other type was reviewed
  and found inapplicable for a domain reason.
- Read models are a separate, equally mandatory pass: a model with dozens of command specs and no
  read-model spec is not scenario-complete. Population, then update or removal where an event
  supersedes a row.
- The why behind each business rule is written down (slice `documentation`), not only the what.
- Good and bad forms of givens, outcomes and rejections: `references/scenario-examples.md`.

### Review questions
- Where is this invariant enforced, and what does the loser of a race see (enforced or declared)?
- What does a client that lost the acknowledgement do with the retry's answer?
- Can facts from other event sources arrive before ours? Does the view tolerate it?
- After a correction or backdated fact, does the view still say something true?
- Cross-context views: are the real source contracts resolved, or is missing evidence a gap?

## Public-event coverage (ESM v9)

Outbound publication needs `when append <private>` and `then <PublicEvent>`:
complete state, incomplete state, unchanged repeat, changed instance, separate
source ids and the exported field boundary. Reference publication is executable;
source-event captures and reached opaque event-target reducers are typed
Unsupported. Read automations-and-translations `references/public-events-and-direction.md`.
For routed producers/observers, cover override versus inherited route, skipped
production route failure, matching/wrong source/stream and unrouted facts;
see toolchain `references/production-routes-and-filters.md`.

## Version skew: reaction cascades and false PLAY0285
(History: the 3.53.1 bundle carries Screenplay 4.137.0, so this skew no longer applies to it.)
Under ESM v6 a command spec's `then` lists the events reactions cascade from it. The compiler
bundled in `cratis` before 3.28.2 (Screenplay 4.60.1) reported a **false PLAY0285** for such
a spec (cli#242); the standalone `screenplay` 4.66.0 and `cratis` 3.28.2 accept it (probed).
Neither checks that a cascade event is *missing*.
- Screenplay 4.61 or later (standalone, or `cratis` 3.28.2 or later): write the v6-correct spec
  (own productions plus cascade); validate with `screenplay <folder> --warnaserror --no-color`
  or `cratis screenplay validate <folder> --warnings-as-errors`.
- Only a `cratis` before 3.28.2 bundle and no standalone tool: keep the command spec to its own productions, specify the reaction in its
  Automation slice (`when append <Trigger>` -> `then <Produced>`), record the pending cascade
  expectation in `STATE.md` as known-incomplete. Never delete the reaction.

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
  on **Screenplay 4.145.0 and later**, propose → review → run with `proposalId` →
  apply. PLAY0285 (impossible `then`) unbinds
  the whole model; no spec runs.
  if not executed, report the actual host/input reason, not a blanket no-route
  claim. Stage's runner is target-engine evidence, never V4; V1/V3 do not prove pass. V5 is "not run" unless rendering was attempted; report admission, publication, build and
  tests separately.
- Match guarantees to evidence: `.play` outcomes, in-process Chronicle scenarios and real-kernel
  tests are not interchangeable (`references/chronicle-verification.md`).
- Reference example `references/berth-reservations.md`: compiles with warnings as errors.

## Route near misses
- Grammar, vocabulary, comparison: `cratis-screenplay-specifications`. Rules being pinned:
  `cratis-screenplay-command-surface`. Projections asserted on: `cratis-screenplay-projections`.
- Reaction, clock and capture scenarios: `cratis-screenplay-automations-and-translations`.
  Invariant placement: `cratis-screenplay-streams-and-consistency`.
- Reviewing coverage critically: `cratis-screenplay-model-review`. C# specs:
  `cratis-specifications-csharp`.

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
