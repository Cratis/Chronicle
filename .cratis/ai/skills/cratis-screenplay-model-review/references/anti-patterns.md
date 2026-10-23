<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/anti-patterns.md -->
# Anti-pattern catalogue

Each entry: **signal** (what you see in `.play`), **why it matters**, **tier**, **fix**.
Shapes are investigation signals, not verdicts: a finding needs a named domain consequence
("two teams will change this read model for unrelated reasons", "a retried payment books twice").
Tiers: C compiler contract, D modeling default, Q review question.
Entries marked (T) are adapted from TrogonStack `eventmodeling-validating-event-models-checklist`
and `eventmodeling-validating-event-models` (see `provenance.md`).

Report each hit in the format of `report-template.md` "Anti-patterns identified": the pattern,
the problem, the checks it violates, the fix, all pinned to one declaration.

## Fact design
| Name | Signal | Why it matters | Tier | Fix |
|---|---|---|---|---|
| Field-copy / CRUD event | `CustomerUpdated`, `AppointmentUpdated`; an event whose properties repeat the creation event; a command that resends the whole entity with no state rule | signal to investigate: it may record only that data changed, not why; if so the change escapes rules attached to the reason (a uniqueness claim, a positive-amount check), consumers cannot react to the reason, history is unreadable (Doomen). A description such as "corrects details" does not fix it. Distinct facts that share a payload shape, and historical generations, are not findings. Major only when business meaning is lost, a rule is bypassed or consumers are ambiguous; historical generations exempt | D | name each business change as its own fact (`AppointmentRescheduled`); never "fix" it by adding the generic event to constraints or projections; a correction names what it corrects |
| Running figures as facts | event carrying totals, balances, counts derived from other facts (totals, balances, ratings, recurring revenue) | rule changes force rewriting history | D | move to a projection. Exception: a decided result that matters later (a quoted price, an assessed fee) is a fact |
| Technical or UI events | `ButtonClicked`, `PageViewed`, `RecordSynced`, `CacheRefreshed` | not business facts; couple the model to implementation (Dymitruk) | D | drop; keep the business fact the action led to, if any |
| Speculative or pending facts | `OrderMayShip`, `PaymentPending` | a fact cannot be a maybe; hides the decision | D | name the decision that happened (`...Requested`, `...Authorized`) |
| Query-like command | command that only returns data | commands decide; reads belong to queries | D | query on a view |
| Rejection as event | `ReservationRejected` emitted for an invalid request | a refused request leaves no fact | D | `validate`/`require` + `then error` |
| Inverse: outcome as rejection | an external refusal modeled as `then error` | someone else's decision IS a fact we learned | D | event in a `Translate` slice |
| Optional covering two situations | one event with an optional member that means "different case" | consumers must branch on absence | Q | two events, if the business names two situations |
| Shared event across identities | same event appended to unrelated kinds of event source | couples lifecycles | D | one owner; others react |
| Id copied to payload | `xId = xId` with `for xId` (PLAY0469) | duplicate source of truth | D | remove unless a consumer needs the value |
| Secret as fact | bearer token, magic link or signed URL in an event | anyone with the log holds the credential | D | record a keyed hash or a reference |
| Clock-relative view state | projected `IsOverdue` | wrong once the deadline passes without an event | D | store the deadline; compare at the edge |
| Mixed-subject PII | one event holding personal data of several people | erasure per person impossible | D | split facts per data subject |

## Boundaries and streams (incl. Dudycz)
| Name | Signal | Why it matters | Tier | Fix |
|---|---|---|---|---|
| Collection stream | `for` a list or "all X" identity; grows forever with unrelated items | contention, unbounded length, no business identity | D | one stream per thing; collection becomes a view |
| Log stream | stream of unrelated technical records | not a business identity | D | observability belongs elsewhere |
| Internal facts published outward | other contexts consume fine-grained internal events | distributed monolith; every internal change breaks partners | D | coarse public or summary event for others |
| Long-lived stream with churning contract | one stream spans years and many contract versions | versioning cost | Q | split lifecycle phases into shorter-lived streams where the business has phases |
| Event shaped for a projection | fields added only to make a view easy | events change for view reasons | D | derive in the projection |

## Dependencies and state (T, translated to Screenplay)
| Name | Signal | Why it matters | Tier | Fix |
|---|---|---|---|---|
| Circular dependencies (T) | slice A's reaction needs a fact only slice B produces, and B's reaction needs a fact only A produces; two features each wait on the other; a projection fed by events that the same projection's consumers produce | nothing can start; the flow is hard to reason about and to specify (a `given` needs the other side first) | D (major when a flow can never start) | redraw the boundary so one side owns the starting fact; consumers read in one direction only; check with `dependencies direction=incoming` and `outgoing` on both ends (checklist F6) |
| Calculation as fact (T) | `RatingRecalculated`, `TotalComputed` with a value that changes whenever source facts change | the value is recomputed, not decided; history must be rewritten when the rule changes | D | the source facts stay; the figure is a read-model field built by a projection (see "Running figures as facts") |
| Shared decision state (T) | many commands `reads` one wide read model and `require` different fields of it; a "god view" doubles as the place every rule looks | commands become coupled to one view; changing it for a screen breaks rules; teams collide | Q (a finding when a change to the view for one reason would change another command's decision) | give each rule the narrowest `reads` it needs, with its own view when the reason to change differs; record the unenforced intent (checklist E3). This is the Screenplay form of "each command owns its own state"; it is not a ban on sharing a view that has one reason to change |
| Persistent decision state (T) | a status or flag that commands write as their own record, or a read model both a projection and a command update; a table mirrored by hand | duplicates the facts, loses the audit trail, drifts from replay | D (major when two writers exist) | state is derived: one builder per read model, commands only produce facts (checklist D7) |
| Hidden event versioning | a payload changed in place, an event renamed with new meaning | history reads differently after the change | D (major) | a new generation or a new event; keep `id "<Old>"` only for a pure rename with the same meaning (checklist C9) |
| Command with no outcome (T) | a command that produces no event, no operation and has no refusal path | nothing happened, so nothing was decided | D | produce the fact, or make it a query, or document the refusal and add the `then error` spec |

## Structural shapes (Dilger: diagnostic signals)
| Name | Signal | Ask | Tier |
|---|---|---|---|
| Left chair | one command producing many events | one decision recognised as several facts (fine), or several decisions in one command (procedural)? | Q |
| Right chair | one read model built from many events | one screen need, or a god view that changes for unrelated reasons? | Q |
| Bed | one form submit or `on submit` that executes several commands in sequence | one user intent split across commands; partial failure leaves what? A screen with several independent `action`s is fine | D (finding when partial failure has a consequence) |
| Book shelf | one slice with many specs while neighbours have few | rules concentrated in one place, or neighbours under-specified? | Q |
| Fan-in hotspot | read-model field drawing on more than about three events | should the view split by reason to change? | Q |

## Screenplay traps (usually defects)
| Name | Signal | Why it matters | Tier | Fix |
|---|---|---|---|---|
| Dropped construct | PLAY0029 warning; misspelled keyword or `type` inside a slice | the construct is silently ignored | C | fix the keyword; validate with warnings as errors |
| Missing `for` | an event production (`produces <Event>` or `produces event`) without `for`. Not operation productions: they are authorable design intent, belong to commands, and take no event `for`, tags or routing | runtimes disagree on the destination | C (executable) / D | `for <identifier>` |
| Unbuilt read-model field | field no projection maps and AutoMap cannot fill | always empty; not reported by the compiler | D | map it or remove it |
| Undeclared removal/append event | `remove with` or capture `append` names an undeclared event | V1 warns (PLAY0166, a failure under warnings as errors); binding fails PLAY0273 | C (executable) / D | declare or fix the name; run V3 |
| Gated command invoked by a reaction without a declared actor | reaction `invokes` a command under a caller policy and has no `runs as system role "<Role>"` that satisfies the gate | the invocation carries no caller: rejected at run time | D (major) | declare `runs as system role "<Role>"` with the least-privilege role the gate admits (ESM v10, Screenplay 4.125.0; Stage 4.89.1 renders role/authenticated trusted actors, but claim-reading/opaque gates still refuse STAGE-ESM-034 and need gap-fill) or record another trusted-path decision - never just delete the gate. A matching `runs as system role` is the admitted trusted path, not a defect |
| `reads` + `produces` reaction | reaction both reads a view and produces | fails binding | C (executable) | invoke a command, or produce without reads |
| Unadmitted read claimed protected | rule "checked" by legacy command or trigger reads | no protected contract or target realization | D (major) | opt eligible command reads into `command reads protected` (v25); inspect shape, absence and conflict cases. the Cratis renderer renders protected reads as Arc read protection once the CLI bundles a Stage that supports it (older bundles refuse them with STAGE-ESM-044; releases in command-surface `references/protected-reads.md`); distinguish reference/semantic-engine evidence from generated-app protection; checklist E3 |
| Caller-supplied state rule | `require currentStatus == "open"` where the caller sends `currentStatus` | the caller decides its own permission | D (major) | read authoritative stored state through admitted protected reads and `require`; mark legacy/refused shapes and target realization as gaps; checklist E3. Uuid ownership compares the caller with the stored owner (`require caller == Document.owner`, v28) |
| `severity warning` as soft rule | rule meant to warn but still rejects | blocks valid requests | C semantics | remove or make it a real rule |
| Relay events | `*Synced`, `*Forwarded` events that only move data | noise, not facts | D | translate once into a business fact |
| External event driving a worker directly | captured external record invokes commands with no local fact | no audit of what was learned | D | append a local fact in `Translate`; automate from it |
| Infrastructure as Translate | DB maintenance, cache or job bookkeeping modeled as external facts | pollutes the domain | D | leave out, or classify as maintenance |
| Projection-level `key` | `key` on the projection, not on `from` | ignored (PLAY0381) | C | move to `from` |
| `#` comment | `#` used as a comment | PLAY0001 at top level; PLAY0029 (dropped) inside a slice | C | `//` or `description` |
| Comment-only rationale | design rationale only in `//` comments | unplaceable comments can be dropped by canonicalizing edits | D | move real reasoning to fenced Markdown `documentation`; keep only the summary in `description` |

## People and flow
| Name | Signal | Why it matters | Tier | Fix |
|---|---|---|---|---|
| Generic or inert persona | "User"; persona without actions or views and no stated purpose | hides who may do what. Read-only auditors with a purpose are legitimate | D | role catalogue with "Cannot: ..." |
| Unmotivated command | command no screen, form, reaction or integration reaches | dead or missing origin | D | add the origin or ask |
| Orphan event | event no view, reaction, constraint or partner uses, with no terminal reason | possibly a missing consumer | Q | ask what the business does with it |
| Missing undo | business can reverse an outcome but no fact records the reversal | corrections become edits | Q | compensating event |

## Strategy anti-patterns
The patterns the Screenplay strategy rejected (code-first over accepted intent, diagrams in
place of semantics, silent authorization weakening, stubs shown as success, managed-output edits,
exactly-once claims without proof, and the rest) are checks R1-R13 in `checklist.md` phase 11,
with their signals and severities.
