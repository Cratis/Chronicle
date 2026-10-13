<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/checklist.md -->
# Review checklist

## Contents

- Phase 1. Element sweep (7 checks, S1-S7; run first)
- Phase 2. Entity walk (5 checks, T1-T5; feeds F1-F3 and scenario coverage)
- Phase 3. Completeness and field lineage (8 checks, A1-A8)
- Phase 4. Ownership and identity (6 checks, B1-B6)
- Phase 5. Event quality (9 checks, C1-C9)
- Phase 6. Events versus views (7 checks, D1-D7)
- Phase 7. Rules and their layer (9 checks, E1-E9)
- Phase 8. Flow and lifecycle (13 checks, F1-F13)
- Phase 9. Personas, reach and authorization (8 checks, G1-G8)
- Phase 10. Views, queries and screens (4 checks, H1-H4)
- Phase 11. Strategy checks (13 checks, R1-R13)
- Closing questions (3, Q1-Q3; each needs an answer in the report; a "no" is a finding)

Eleven phases, 89 checks and three closing questions. Each phase header says how many checks it holds; the report lists
every phase with its count and, per check, a status, the element and the evidence
(`report-template.md`). Each check names what to look at, the Screenplay evidence source and the
tier:

- **C** compiler contract: report as a defect when the target mode needs it.
- **D** modeling default: a deviation needs a recorded reason, otherwise it is a finding.
- **Q** review question: a finding only with a named domain consequence.

Statuses: `PASS`, `FINDING #n`, `N/A: <domain reason>`, `NOT RUN: <reason>`. A check never
skips silently. Phases 1 and 2 are mechanical and run first; judgement follows.

Phase layout adapted from TrogonStack `eventmodeling-validating-event-models-checklist`
(see `provenance.md`).

## Phase 1. Element sweep (7 checks, S1-S7; run first)
Write one line per element in working notes, then copy every flagged line into the Checks
table and the findings. A finding names its elements; lumped remarks ("add denial coverage",
"most gated commands") do not count and fail Verify. A description, comment or spec name never
excuses a flagged line: the event name, payload and specs carry the business meaning.

| Id | Element | Line to write | Flag when | Tier |
|---|---|---|---|---|
| S1 | Gated command or query | `<name>: gate <policy> (construct/feature/module); denied spec <name or NONE>` | NONE | D (major when the gate protects another person's data or a refusal the business named) |
| S2 | Event | `<name>: verb <verb>; properties <list>; same as creation event <name>? yes/no` | same properties as the creation event, or a generic verb with most entity fields (not a historical generation) | D (field-copy signal; a finding when the reason is hidden); major when business meaning is lost, a rule is bypassed or consumers are ambiguous, else minor |
| S3 | Command or event name | `<name>: form-named? yes/no` | `Update/Edit/Save/Set/Change/Manage<Noun>` command, or `<Noun>Updated/Changed/Edited/Saved/Modified` event, whatever the payload size; names of one business change pass | D (minor; major under the S2 conditions) |
| S4 | Event, one property | `<name>: one property, generic verb` (`EmailChanged`) | always ask the reason | Q (minor at most) |
| S5 | Command | `<name>: produces <n> (<names>); state change <from -> to / none>; requires <rule / none>` | n >= 3 or restated facts; a non-creating command with no state change and no rule (it produces no new fact and decides nothing) | Q / D |
| S6 | Event property | `<event>.<prop> equals event-source/identifier? yes/no; consumer <name or none>` | yes and no consumer (PLAY0469) | D |
| S7 | Slice | `<kind> <name>: parts present; specs <names or no specs>` | StateChange without command, produced event or reachable origin; StateView without projection, query or reader; Automation without trigger or invoke/produce; Translate without inbound source or local fact; any slice with `no specs` | D (skip when documentation says it is intentionally partial) |

Field-copy test: list each kind of event source's events with their property sets side by
side. An event that repeats the creation event's properties may record only that data changed,
not why: a signal, so confirm the hidden reason before reporting. The fix names each business
change (`BerthReassigned`, `BerthLengthLimitRaised`), one event per reason, each with its own
rules and consumers. A shared payload shape alone is not a defect: full generations repeat the
prior shape on purpose and different facts may look alike. Screenplay 4.127.0 MCP
`find-modeling-smells` reports advisory questions (SMELL001–SMELL005) for generic
names, duplicate shapes and wide flows. It supplements the sweep, never changes
a compiler verdict or proves a domain defect.

## Phase 2. Entity walk (5 checks, T1-T5; feeds F1-F3 and scenario coverage)
Per kind of event source, in written form (adapted from TrogonStack
`eventmodeling-validating-event-models`):
1. T1 First fact and the command that produces it.
2. T2 Allowed changes: `<command>: <from-state> -> <fact> -> <to-state>`.
3. T3 Refused changes: `<command> in <state>: refused, message <text>, spec <name or NONE>`.
4. T4 Final states and what is still allowed after them.
5. T5 Alternative paths walked separately: cancel, fail, correct, reverse, expire.

Flag: a command or fact missing from the table; a fact possible before the first fact; a
refused change without a `then error` spec; a path the business has but the model lacks; a
command that produces neither an event nor an operation and documents no refusal (a command with no outcome).

## Phase 3. Completeness and field lineage (8 checks, A1-A8)
Build a matrix for the scope: one row per read-model field, event field and command input. Use
the format in `worked-example.md`.

| Id | Check | Evidence | Tier |
|---|---|---|---|
| A1 | Every read-model field maps from an event field, `$eventSourceId`, `$eventContext`, a literal, or a documented derivation (`count`, `add ... by`) | projection blocks; AutoMap name matches (on by default) | D (opt-in PLAY0534 detects known missing origins; unknown coverage still needs a walk. Absent mapping targets warn at V1 with PLAY0514, tier C for source validity) |
| A2 | Every event field comes from a command input, `$context` value, literal, capture field or reaction mapping | `produces` mappings, capture `map`, reaction `invokes` | C for undeclared names (warning), D for meaning |
| A3 | Every command input is supplied by someone: screen form field, reaction mapping, capture, or a caller who knows it | screens, forms, reactions | D |
| A4 | Every command input ends in an event field or is used by a rule | `produces`, `validate`, `require` | Q (an unused input is often a missed fact) |
| A5 | Every event has a consumer (view, reaction, constraint, external contract) or a stated terminal reason | MCP `dependencies direction=incoming` | Q |
| A6 | Every declared view is read by a screen, a reaction or an external caller | screens `data ... via query`, reaction `reads` | Q |
| A7 | Data called "missing" was searched for elsewhere in the model first | `search-declarations` | procedure |
| A8 | Step contracts hold: what each step needs (precondition) is what the previous step's fact provides; each step's postcondition names the fact and its fields | slice order, event fields | D |

## Phase 4. Ownership and identity (6 checks, B1-B6)
| Id | Check | Evidence | Tier |
|---|---|---|---|
| B1 | Each event production states `for`; the destination is the right business identity. Operation productions (`produces operation <Name>` or a reference to one) take no event `for`: they are valid design intent with a documented execution gap (operations.md), not a defect and not an outcome-less command | `produces ... for` | C (executable), D (design) |
| B2 | An event belongs to one kind of event source; the same contract is not appended to unrelated identities | `for` targets across commands | D |
| B3 | The event source id is not copied into the payload unless a consumer needs it as a value | PLAY0469 information | D |
| B4 | A stream is one business thing, not a collection or a log | event names, `for` types | D (see anti-patterns) |
| B5 | Each persisted event is declared once, in its producing slice; renames keep `id "<Old>"` | declarations, `id` pins | C/D |
| B6 | Each module owns its events: another module consumes them through a reaction, a read or a capture and does not append them | module and feature documentation, `produces` across modules | D |

## Phase 5. Event quality (9 checks, C1-C9)
| Id | Check | Evidence | Tier |
|---|---|---|---|
| C1 | Past tense, business verb, names a fact a domain expert recognises | event names | D |
| C2 | A fact, not a request, a running calculation, a UI action or a technical step | names and payloads | D |
| C3 | No speculative or pending events ("MayBe...", "...Pending") where a decision is meant | names | Q |
| C4 | No nullable or optional event property by default: a required value, or a separate event for the second situation (CHR0012; a `null` in a specification value is PLAY0350); a kept `optional` has its reason in `documentation` | `optional` members | D |
| C5 | Personal values marked bare `pii`, operational secrets `secret`, not mixed across data subjects; no protected concept in identifiers, destinations, stream-id types/parts or route sources (PLAY0515, CHR0034/0052). Event `subject` is required unprotected scalar and report-only (PLAY0590–0595/0270), not runtime lineage. Special/criminal qualifiers belong on the value, purpose/basis/retention on declared processing uses; inspect `--check purposes`/processing record, not a legal verdict. Legacy markers are PLAY0565; `pii personal` repeats one marker (PLAY0653) | concepts, subjects, purpose coverage | D; removal to pass tools = critical |
| C6 | No secret or bearer values as facts (tokens, magic links, signed URLs): record a keyed hash or a reference | event payloads | D |
| C7 | No two events of one kind of event source mean the same thing (a shared property set is a signal, not a defect: distinct facts may share a shape and historical generations are exempt; major only when meaning is lost, a rule bypassed or consumers ambiguous); one fact is not recorded twice (aggregate event plus per-item fan-out) | event declarations side by side | D |
| C8 | Corrections are new facts that name what they correct; redaction and crypto-erasure are compliance tools, not corrections | event names | D |
| C9 | A changed event contract is a generation or a new event with a recorded reason; no past meaning is reinterpreted through a rename or payload cleanup | `id` pins, generations, documentation | D (major when history changes meaning) |

## Phase 6. Events versus views (7 checks, D1-D7)
| Id | Check | Evidence | Tier |
|---|---|---|---|
| D1 | Values that can be worked out from stored facts live in views | projections | D |
| D2 | Decided calculations that matter later (quoted price, assessed fee) are recorded as facts | events | D |
| D3 | The rule behind a view could change without rewriting stored facts | projection vs event payload | Q |
| D4 | Views do not join other views; each is rebuildable from facts alone | projections | D |
| D5 | No clock-relative state in a view (`IsOverdue`): store the deadline, compare at the query or UI edge | projections | D |
| D6 | Every `from` event in a projection sets or affects at least one read-model field (AutoMap counts), or the reason it stays is recorded | projections | D |
| D7 | Decision state is derived, never kept as its own record: a read model has exactly one builder (a projection or a reducer), and no command or reaction writes view state except by producing a fact | projections, reducers, `produces` | C (two builders is PLAY0191) / D |

## Phase 7. Rules and their layer (9 checks, E1-E9)
| Id | Check | Evidence | Tier |
|---|---|---|---|
| E1 | Value formats on `concept`s; input rules on the command; uniqueness as `constraint`; access as `authorize` | declarations | D |
| E2 | Every refusal has a message a person understands; constraint messages copied verbatim in specs | `message`, specs | D |
| E3 | A state-dependent rule is `reads <View>` + `require <expr> message "..."` as stated intent, and the slice `description` marks it NOT enforced in the model today (PLAY0268/0271 at binding expected in design mode) with its target named (`[ProtectedDecision]` + `DecisionRead<T>`, Chronicle DCB / `concurrency` scope, or a constraint). Not: caller-supplied copies of state, attestation booleans (`confirmsX == true`), rules in `handler` / `implementation hint` prose, or `reads` treated as protection | slice `description`, `require`, `handler` | D (faking or hiding = major) |
| E4 | `severity warning` is not used as "soft" validation (it still rejects) | validate lines | C semantics |
| E5 | Preconditions are explicit; nothing relies on "obviously you can't" | descriptions, specs | Q |
| E6 | Each invariant has an atomic enforcement point (name the storage provider where constraint admission matters); the race loser's outcome is specified | constraints, specs | D |
| E7 | Append-time constraints on a claim cover every event that sets the claimed value; value invariants live in the concept; command-specific conditions are reviewed per path, not copied blindly | constraints, concepts, producers | D |
| E8 | No rule exists only in `description` or `documentation` when a layer could express it (neither reaches rendered code); prose names intent, not enforcement. Real reasoning belongs in documentation, finalized at P3's end, summaries in description | metadata vs layers | D (minor; major for renderable scope) |
| E9 | A decided refusal is a `then error` or `then denied` specification (permanent behavior), never a lingering open question; only a genuinely unanswered question counts as open, and resolving it means answering it | specs, `STATE.md` questions, descriptions | D |

## Phase 8. Flow and lifecycle (13 checks, F1-F13)

Review PLAY0516 backward-flow and PLAY0517 sibling-cycle findings as
**presentation-only** information: they do not fail `--warnaserror`, change
execution or prove a business deadlock. An ordering root is required; without
one the folder check is skipped. A real F6 defect needs a named domain
consequence, not just the diagram's cycle. Suggested graph order and safe
PLAY0516 moves preserve executable bytes/identities. Source: Screenplay
`v4.125.0:Documentation/screenplay/imports.md`, `diagnostics.md`.

| Id | Check | Evidence | Tier |
|---|---|---|---|
| F1 | Each entity has a first fact, a state-transition table (including refused transitions) and terminal states | slices, constraints, `description` | D |
| F2 | No impossible sequence (a fact that requires one that can never precede it) | constraints, specs | D |
| F3 | Cancellation, failure and correction paths exist where the business has them | slices | Q |
| F4 | Automations: trigger, todo/work item if work can be pending, a closing fact, retry and once-only story | reactions, views | D (`cratis-screenplay-automations-and-translations`) |
| F5 | External facts enter through `Translate`; in-app follow-ups through `Automation` | slice kinds | C semantics |
| F6 | No cycle where fact A's reaction needs fact B and B's needs A, or two features each wait on the other | reactions, MCP `dependencies` | D (redesign the boundary) |
| F7 | Each slice's consumed events and their producing slices are known; a slice calls another's command only through `invokes` | reactions, `reads` | Q |
| F8 | Every Automation has a pending-work view (a todo list: membership is the pending work), or a written reason a direct effect needs none (immediate, internal, cannot fail). A "simple relay" is not exempt from the question | reactions, `reads`, views | D |
| F9 | A worker's pending-work view is opened only by our own facts. An outside fact that reaches a decision directly means the translation step is missing: split it into a `Translate` slice plus the worker | reactions, captures, `dependencies direction=incoming` | D (major when an external system can trigger domain work with no translation) |
| F10 | A translation that holds real pending work has a completion or terminal disposition; an artificial queue that never closes only hides recovery (omit it when nothing is pending). The local fact is named for its business meaning, never a mechanical transport suffix (`<X>Synced`, `<X>SignalReceived`, `<X>RequestReceived`); a business fact such as `PaymentReceived` is fine | `Translate` slices, views, event names | D |
| F11 | A worker after a translation adds a distinct local decision, obligation or effect (an invariant, a choice, data to compute, a send or other external effect). If its identity, meaning, logic and effects add nothing the translated fact did not already carry, it is a redundant stage: remove it and project downstream views from the fact | worker reaction, commands, events | D |
| F12 | A worker's pending-work view has a closing path for its own result fact, for cancellation and for supersession, and no `status` field: membership in the list is the state | views, projections `remove with` | D (major when work can never leave the list) |
| F13 | Every command has a reachable, supported origin: a screen or form action, or a reaction to any modeled event, clock or application trigger (a capture starts a process by appending facts; a later command is reached through a reaction). No unmotivated command; two commands are not chained without such an origin between them | slice order, origins, triggers, captures | D |

Automation-chain audit (F8-F13): enumerate every Automation and Translate slice with its
location and write one line each: pending-work view, opened by, closed by, result (`complete`,
`open: <decision>`, `blocked: <gap>`). An undeclared opening or closing fact is an unresolved
dependency, never skipped and never called complete. The per-chain questions are in
`cratis-screenplay-automations-and-translations` `references/audit-format.md`; adapted from
Nebulit (`provenance.md`).

## Phase 9. Personas, reach and authorization (8 checks, G1-G8)
| Id | Check | Evidence | Tier |
|---|---|---|---|
| G1 | Every command has at least one reachable and authorized origin, and no command is attributed to a generic "User" | screen `action`, forms, reactions, persona policies | D |
| G2 | Personas have a purpose and say what they cannot do; read-only roles are legitimate; every persona has at least one command or one view (mechanical evidence: `--check personas`, PLAY0574 to PLAY0576) | `persona` descriptions | D |
| G3 | Persona policy lists cover every gate on the path (module, feature, construct) | policies | C semantics (board placement) |
| G4 | A command invoked by a reaction is not gated by a caller policy, unless a trusted-path decision is recorded | reactions, `authorize` | D (silent = major) |
| G5 | Row scoping (whose data) is part of the view, not a filter the caller supplies | queries | D |
| G6 | Every persona Cannot line resolves to an executable gate (`authorize` policy, or ownership `claim "x" matches subject\|<command property>`) and a `then denied` spec whose caller is synthesized with `given caller as <Persona>` or explicitly carries the effective-gate roles/claims; otherwise recorded as a gap. Persona text is intent only | personas, policies, specs | D |
| G7 | Every command and query under an inherited module/feature `authorize` has its own `then denied` spec; authorization shared by a feature is declared at feature/module level so new paths inherit it | policies, specs | D |
| G8 | Ownership/claim targets are a supported scalar (text, Uuid, Date or DateTime render; a numeric or Boolean target is always denied; other shapes and a command without exactly one identifier are STAGE-ESM-015 at Stage 4.73.1): noted | policies | C (renderable mode) |

## Phase 10. Views, queries and screens (4 checks, H1-H4)
| Id | Check | Evidence | Tier |
|---|---|---|---|
| H1 | Query shape follows the business view (list where people scan; lookup where they open one) | queries, screens | D |
| H2 | Executable scope: a keyed `=> RM optional` query with `by` named like a read-model property, or a collection (`=> RM[]`, observable too) with caller-supplied `by` and/or equality filters (ESM v14 since 4.143.0). `from`, single-result filters, opaque `scoped to`/`performer` and composite `by` remain PLAY0268. The projection routes by the same identity (a `from` key or an event property, or `$eventSourceId`; `$eventSourceId` is not mandated). Filtered models: CLI 3.57.0 / Stage 4.89.1 admit through v15, including supported v14 equality filters. Renderable scope only: Stage-specific identity/filter limits (Stage 4.89.1 renders a keyed optional snapshot lookup and observable collection queries; any other query is `STAGE-ESM-010`) | queries, projections | C (executable); C (render admission, renderable only) |
| H3 | One builder per read model; projection `key` only on `from` | projections | C |
| H4 | Screens show one actor's point of view; a form submits one command | screens, forms | D |

## Phase 11. Strategy checks (13 checks, R1-R13)
The rejected anti-patterns of the Screenplay strategy, as review checks. They apply to the
model, the proposal or edit request, the diff under review and the claims in a report. Each
cites its signal; a hit is a finding at the stated severity. An item that does not apply to the
artifact is `N/A: <reason>`.

| Id | Check (the rejected pattern) | Signal to look for | Severity |
|---|---|---|---|
| R1 | Code-first change that silently outranks accepted model intent | behaviour added in code with no `.play` counterpart; the model edited to match existing code; the model left stale after a code change; optional structural evidence: `cratis screenplay conform <model-root> --project <path>` (CLI 3.42.0+, exit 0/1/2, exact structural addresses only, never a behavioral or V4 verdict; MSBuild runs, so a trusted clone) | major (critical when the code contradicts a protection) |
| R2 | A diagram, board or document in place of executable semantics | a rule only in a board note, a description or a wiki page; no property, validation, constraint or spec behind it (E8) | major |
| R3 | Claims of universal low-code, lossless recovery or target parity | words like "fully generated", "lossless", "any target" in a report or description | minor; major when it hides a gap |
| R4 | Framework machinery in the domain vocabulary (saga, aggregate, passive objects, HTTP or broker detail) or syntax wanted because a framework API is common | declarations named after saga, handler classes, topics, endpoints; a request to add language syntax to mirror a framework API | minor; major when it shapes the model |
| R5 | Silent query broadening, authorization weakening, guessed identities, unprotected-read fallback | a query that lost a filter or `by`; a policy removed or loosened to compile or render; an identity assumed instead of resolved | critical |
| R6 | Generated TODOs, defaults or stubs presented as success | TODO, placeholder values, default branches in a "complete" slice or a render report | major |
| R7 | Editing managed output, or mixing generated and user-owned code without a separate contract | edits under Stage-managed paths; hand code in rendered files; no named gap-fill seam | major |
| R8 | AI generation during builds or inside the pure render planner | a pipeline or script that calls a model during build or render | major (delivery review only) |
| R9 | Automatic model mutation from recovered code or observed runtime behaviour | extracted candidates merged without an accepted, corrected or rejected state and provenance | major |
| R10 | Historical events reinterpreted through a rename or payload cleanup | a changed event with no generation or `id` pin story (C9) | major |
| R11 | Exactly-once claims inferred from replay controls, delivery ids or reference-clock execution | "exactly once" without the enforcement point proven (E6, F4) | major |
| R12 | A green compile, an attractive board or a closed issue taken as proof the application works | a report that states "works" from V1 alone; a verdict line with no evidence | major (use `NOT RUN`) |
| R13 | Making modeling mandatory where the repository has not opted in | a finding that fails a repository for lacking a model, or forces it onto framework or brownfield work | the finding is withdrawn; note it |

## Closing questions (3, Q1-Q3; each needs an answer in the report; a "no" is a finding)
- Q1 Could someone new to the domain follow the main flow from the model alone in a short
  sitting? If not, name what is in the way (minor unless it hides a rule).
- Q2 Could a business rule or calculation behind a view change without rewriting stored facts?
  If not, name the event and field (D1/D3).
- Q3 Could the scope be delivered in the target (modeled and rendered, or hand-written to the
  `.play` contract) without anyone having to guess a rule? If not, name the unresolved rule.
