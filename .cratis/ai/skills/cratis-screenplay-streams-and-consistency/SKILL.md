---
name: cratis-screenplay-streams-and-consistency
description: "Decide Screenplay stream identity and consistency: one business identity per event source, where each invariant is enforced atomically, constraints versus recorded state-dependent rules, races, retries and dedup scope, event granularity, evolution (generations, id pins, new events) and module or team boundaries. Use for identifier/for, constraints, splitting streams or modules, ownership of interfaces, or changing persisted events. Not for: projection or query syntax (use `cratis-screenplay-projections`), reactions and captures (use `cratis-screenplay-automations-and-translations`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/SKILL.md -->

# Streams and consistency

Decide what each stream *is*, which rules hold atomically and where, and how events may change
later; then express that in `.play` truthfully. The model must say which invariants it protects,
which it only records, and what a retry, a race or a replay does. Grammar lives in the construct
skills; this skill owns the decisions. The `.play` model is the source of truth: a rule that
lives only in code, or only in a description, is a finding, not a solution.

**First append into a scope (Chronicle v19.50.0, version-qualified).** The default optimistic
strategy compares the scope's tail sequence number. When nothing matches the scope yet (the
first event on a new event source, or the first event of a new source type, stream type or
stream id), there is no tail and the append is **not checked**: two writers opening the same
source or narrowed partition can both succeed. This is the append most exposed to a race, so a
concurrency scope alone does not protect "only one writer may open this". For a target that
needs it:
- enable the check, application-wide with `ConcurrencyOptions.CheckFirstAppendIntoAScope`
  (default `false`) or per append with `ConcurrencyScopeBuilder.ExpectingNoMatchingEvent()`,
  and handle the `ConcurrencyViolation` it can now raise; or
- use a unique event constraint, which the kernel enforces for every writer whether or not the
  append declared a scope (the model's `unique event`).
Require target evidence that the check ran: a concurrent first-append scenario against a real
Chronicle, asserting one append wins, one is rejected, and `IAppendResult.ConcurrencyCheckPerformed`
is `true` (a skipped check and a passing one look identical otherwise; a current client against
an older kernel always reads `false`). This was source- and documentation-inspected, not
concurrency-tested here. The Stage scaffold pins Chronicle client/kernel and Arc releases (see `cratis-stage-rendering-and-sandbox` `references/rendered-application.md`;
the CLI bundle's Chronicle is in `cratis-screenplay-toolchain` `references/versions.md`); the APIs exist for gap-fill, not as automatically rendered enforcement.

## Verified product sources

| Product | Pin | Used for |
| --- | --- | --- |
| Screenplay | v4.68.0 (`79801bf`) | ESM v7 generated values (`generated identifier`, decision 0026) and decision 0025's numbering; the diagnostics below were read at v4.66.0 (`c89198b`): `identifier`, `for`, constraints, generations, `id` pins, diagnostics PLAY0019/0135/0268/0271/0273/0391-0393/0446-0449/0469/0471 |
| Chronicle | v19.50.0 (read; first read at v19.32.0 `f17a2ff`) | CHR0012, CHR0034; open defects #3744, #4123, #4131 (still open) |
| Arc | v22.72.0 | `[ProtectedDecision]` and `DecisionRead<T>` (since v22.39.0) |
| Stage | v4.89.1 | rendered apps pin the Arc and Chronicle client/kernel listed in the Stage skill's `references/rendered-application.md`; protected-read APIs available to gap-fill. Historical 4.24.2 admission probe: ESM v4, evolved events refused with `STAGE-ESM-026` |

Full pin table: `references/versions.md` in `cratis-screenplay-toolchain`. Re-check the three
Chronicle issues before relying on a constraint in a later release.

## When / when not
- Use: choosing `identifier` and `for`; adding or naming a `constraint`; a rule that depends on
  earlier state; "can two people do this at once?"; retries and duplicate requests; splitting or
  merging streams, events or modules; adding, renaming or re-meaning an event; who owns which
  interface; reviewing collection- or log-shaped streams.
- Not for: projection and query syntax, reactions and captures, writing the specifications
  themselves (`cratis-screenplay-scenario-coverage`), Chronicle C# mechanics
  (`cratis-chronicle-event-modeling`). See Route near misses.
- Model-first applies: when an accepted model covers the scope, change the model and its
  specifications, not code. Do not weaken authorization, `pii` or rules to make a model compile.

## Interview phase
**Skip if** the user or the model already states the stream identities, who owns each module, how
much autonomy each side has, the external systems, and rough growth. Otherwise ask only what is
missing, one question at a time, with options. **Unattended:** assume the most conservative
option, mark it `ASSUMED` in the slice or module `documentation` and in the session `STATE.md`
(`.ai-work/screenplay/<model-slug>/STATE.md`), and continue.

1. **Instance.** "What is the real-world thing this stream is about, and when does it start and
   end?" Follow-up: if the answer needs "and" for two lifecycles, ask which facts live
   independently of the other.
2. **Competition.** "Can two people do this at once, and what must never happen then?" Follow-up:
   for each answer ask whether it is a uniqueness, a count or a state rule.
3. **Retry.** "If the reply is lost and the client sends the request again, what should the
   second request see?" Follow-up: is the identifier chosen before the first attempt?
4. **Ownership** (full protocol and options in `references/boundaries.md`). Who decides these
   rules and operates them when they fail; how much can each side move alone; which external
   systems take part and who owns each adapter; what delay or disagreement the business
   tolerates. Follow-up on technical-function teams: which business capability does each serve?
5. **Growth.** "Roughly how many events per instance per year, over how many years, and which
   commands read the stream versus a view?" Follow-up: if the estimate is large, check the
   identity before anything else (`references/stream-patterns.md`, Growth and snapshots).
6. **Change.** "Has this event ever been stored in a running system?" It decides whether an edit
   is ordinary or contract evolution.

## Procedure
1. **Establish identity and membership.** For every `concept XId` used as an `identifier`, write
   one sentence: "one business identity per stream: one stream per <business thing>; it answers
   <question>". Run the event-by-event check in `references/stream-patterns.md`: inspect every
   producer and destination that can write its history (commands, reactions, captures), say why
   each event belongs, and decide retain, split, view instead, or unresolved, by lifecycle and
   invariant consequences, not names or length. For Chronicle keep event-source identity,
   namespace, process-stream routing and concurrency scope apart
   (`references/chronicle-boundaries.md`). Declared `eventsource`/`stream`/`streamId` and
   command/specification routes bind and reference-execute as ESM v8 on 4.125.0.
   The pinned CLI (toolchain `references/versions.md`) reaches Stage construct checks through v15; keep the routing
   contract when target admission requires gap-fill. Forms/boundaries:
   toolchain `references/sources-and-streams.md`. A `generated identifier` (ESM v7, standalone 4.68.0 and later; Stage 4.89.1 renders supported UUID/String strategies and v15 freshness; unsupported types/results still refuse -028/-038) is
   generated fresh on every acceptance: it is not a retry identity, and generated values give no
   idempotency, retry or deduplication guarantee. When a retry must find the same stream, the caller
   supplies the identifier before the first attempt.
2. **Account for every invariant.** One row per rule in the table from
   `references/consistency-and-concurrency.md`: rule, authoritative inputs, all paths that can
   affect it, intended atomic decision point, exact construct, status (`enforced` / `recorded` /
   `eventual by choice`). Placement:
   - only the command's own inputs: concept `validate`, command `validate`/`require`;
   - supported uniqueness and exclusion/release rules: a named `constraint` (`unique ... on`,
     `unique event`, `released by`); list every event that sets the claimed value;
   - a state-dependent rule not reducible to those: write `reads <View>` + `require <expr>
     message "..."` as stated intent (PLAY0268/0271 at binding are expected in design mode) and
     mark it **NOT enforced in the model today** in the slice `description` with the named target,
     and record requirement, missing guarantee and race consequence in the slice `documentation`. Add a `STATE.md` row
     with an owner. A general "some rules are unsupported" note is not enough.
   - Forbidden: a caller-supplied copy of state in `require`, a boolean attestation input
     (`confirmsX == true`) standing in for a rule, and rules hidden in `handler` or
     implementation-hint prose. An unguarded materialized read is unsafe for a protected
     decision; that is not "read models never drive decisions".
3. **Play the races and retries.** For each enforced or recorded rule write the concurrent case
   (two commands at once), the retry case (same request again after a lost reply) and the
   duplicate case (same intent, new id). State the outcome and which construct produces it.
   Check the **retry identity**: the identifier is chosen before the first attempt and reused on
   retry, or the retry creates a second stream.
4. **Set granularity.** One decision usually records one event; several facts are fine when each
   is a fact the business recognises. Different meanings get different events, not a flag with
   nullable siblings. Event properties are required by default (CHR0012, PLAY0350): an optional
   detail is a separate event, and any deviation is justified in the event's `documentation`. One data
   subject per event; never personal data, a secret, or a `pii`/`secret` concept as
   identifier or subject (CHR0034, CHR0052, PLAY0515/0592). Use a surrogate identity
   and protected payload. A stream identity and an event's data subject may differ:
   trailing event-property `subject` records that lineage but is report-only
   (PLAY0270), not runtime propagation or generated C# `[Subject]`. Without a
   supplied subject, the runtime defaults to the event source; see command-surface
   `references/compliance.md` for allowed types and the policy/encryption distinction.
5. **Plan evolution** with `references/evolution.md`: classify each event change (additive,
   meaning change, rename, removal, split or merge) and write its compatibility scenarios before
   editing. Pin persisted/public enum member numbers before reordering or removing
   members; never renumber or reuse a published number. Positional pins preserve
   executable bytes; non-positional pins need ESM v12 consumer admission.
   Renames and other identity-affecting edits are made by the identity owner (the
   identity procedure in `cratis-screenplay-modeling-lifecycle`).
6. **Test ownership and interfaces** with `references/boundaries.md`: ownership interview,
   interface and processor inventory, one real interaction walked across each boundary, coupling
   check. Keep team, module, stream and deployment boundaries distinct; inbound facts via
   Translate; unresolved decisions stay visible.
7. **Record and gate.** Durable rationale in feature and slice `documentation`; open consistency
   decisions and capability gaps in `STATE.md`. Then the gate below.

## Rules
**Compiler contracts** (diagnostics at Screenplay v4.66.0 and v4.68.0; capability per tool in `versions.md`)
- `identifier` on an event property is PLAY0019; the source id is never payload by declaration.
  Copying it as a payload value is PLAY0469 (information, warning for inline events).
- In the executable model a production's `for` must resolve to the command's scalar identifier:
  one command writes one stream declaratively (a handler is never admitted).
- Constraint names are application-wide identities (PLAY0392); properties must be declared
  directly on the event (PLAY0391); never mix event and property rules under one name (PLAY0135);
  `ignore casing` is not allowed on `unique event` (PLAY0393).
- Generations are declared in full, numbered 1..N without gaps, in the same slice
  (PLAY0446-0448). Referencing a removed property is PLAY0273; a `given` needing a historical
  shape is PLAY0449. An `id` pin equal to the event's current name is PLAY0471 (information).
- `reads` is PLAY0271 and `require` over read-model paths is PLAY0268 at binding. A `concurrency`
  block compiles but is PLAY0271 at binding. Keep both when they state real intent; they are not
  protection. `description` text never reaches rendered code: a rule that lives only in prose is
  unenforced there.
- `pii` and `secret` bind as ESM v13 on standalone 4.142.0 (since 4.141.0) and the pinned CLI;
  enum/identity compliance and protected events with explicit subject marks still
  refuse PLAY0268 (command-surface `references/compliance.md`). Legacy markers are
  deprecated (PLAY0565). Never remove protection to pass a tool. PLAY0515 also refuses
  scalar/composite stream-id types and route mapping sources, including nested paths.
- At 4.127.0 stream-id literals must be nonempty, well-formed NFC text (refused,
  not normalized); integer ids in Double mode stay within ±(2^53−1). Exact mode
  has no such bound but is unadmitted. PLAY0504 is an error for route/destination
  nominal type mismatch. Composite ids use at least two named scalar parts,
  complete named mappings and canonical comparison, not guessed legacy splits.
  Stored key-schema changes require a new stream identity or migration; see
  toolchain `references/sources-and-streams.md`.
- Chronicle (open at v19.50.0): the SQL and InMemory providers do not settle unique claims
  (#3744); index updates run after commit and may fail silently (#4123); composite values can
  collide on the separator (#4131). A declared constraint is a declaration, not a runtime test.
- `[ProtectedDecision]` is available in Arc v22.39.0 or later, including Stage 4.73.1's
  rendered package set. It is not emitted protection: gap-fill must enroll an admitted
  decision read, or redesign the stream/identity when the shape is refused
  (`references/consistency-and-concurrency.md`, section 3).

**Modeling defaults** (deviate with a recorded reason)
- One business identity per stream; cross-stream references are payload properties.
- Copy the source id into the payload only when a consumer needs it as a value.
- Give each invariant an authoritative atomic decision point and inspect every path that could
  violate it. A constraint violation rejects that command's whole append set; a reaction cascade
  is not one transaction. Declared semantics are not a passed concurrency test.
- Match duplicate protection to scope. A property constraint lets a source re-claim its own
  value; use `unique event` for a fact that is genuinely once-only in its lifecycle, without
  blocking legitimate repeated operations just to make retries safe.
- No calculated events: a value recomputed as source data changes is a read model; a figure
  the business recorded at a point in time (a quoted price) is a fact. Keep an unanswered
  choice as an open question in `STATE.md`; write a decided rejection as `then error`
  (`references/stream-patterns.md`, Core rules that bear on stream design).
- Prefer short business lifecycles (a new stream per period, case or attempt) over an eternal
  stream, when the business has natural ends.
- Compatible change is a new generation; changed meaning is a new event. Corrections are new
  facts, never an edit of history. Redaction and crypto-erasure are compliance operations.
- Team and system often align, but not by law: decide modules on ownership, language and change
  cadence, and record where they differ.

**Review questions** (findings only with a domain consequence)
- What single question does this stream answer? Does it grow for the right reason?
- What happens when two people do this at the same moment? When the reply is lost?
- Is any stream a collection or a log wearing an instance's name (decision tree and red flags in
  `references/stream-patterns.md`)?
- Does an optional property cover two situations that the business names differently?
- Does a consumer in another module depend on an event we treat as internal?
- Could this calculated value matter historically (price, assessment)? Then it may be a fact.
- Is "eventually consistent" here a business choice, or an unexamined gap?

## Gate
Do not report done until all hold; otherwise report what is open:
- Every identifier has its identity sentence, the event-membership table and a justified decision.
- Every rule has an invariant-table row with an exact construct or limitation reference.
- Every `recorded` rule has its NOT-enforced marker in the slice `description`, and its requirement,
  missing guarantee, race consequence and intended enforcement owner in the slice `documentation` and `STATE.md`.
- Every changed event has compatibility scenarios; identity changes were made by the identity owner.
- Every changed boundary has a named owner (or `ASSUMED`), callers, processors and contracts.
- Verdicts are stated V1-V5 per `cratis-screenplay-modeling-lifecycle`, each a result or "not run".

## Verify
- Run the compile check on the model (V1, warnings as errors) and state the tool and version;
  say what was not run. Parsing and binding do not execute specifications, and sequential
  specifications do not prove race safety.
- Value uniqueness: a competing-claim spec (another source, `when <Command>`, earlier fact as
  `given`); once-only facts: the same source repeating; release and reclaim when declared. Pin the
  fixed message with `then error`. Report target-side concurrency evidence separately.
- Retry answers cover a lost acknowledgement, reuse of the operation identity, conflicting
  payloads and duplicate intent under a new identity, where applicable.
- Read `references/streams-example.md` when designing stream membership, constraints or a recorded state-dependent rule; read `references/evolution-example.md` when planning a generation, identity-preserving rename or change of event meaning. Examples that compile (`references/streams-example.md`, `references/evolution-example.md`):
  `bash` the repository's compile check over this skill folder.

## Route near misses
- Construct grammar: `cratis-screenplay-command-surface` (identifier, `for`, constraints),
  `cratis-screenplay-projections`; compiler facts and traps: `cratis-screenplay-toolchain`.
- Chronicle C# compliance and event-source ids: `cratis-chronicle-event-modeling`.
- Reactions, todo lists, captures, external facts: `cratis-screenplay-automations-and-translations`.
- Commands, read models and lineage: `cratis-screenplay-slice-design`. Specs:
  `cratis-screenplay-scenario-coverage`.
- Reviewing someone else's model: `cratis-screenplay-model-review`.
- Rendering and the code left to write after it: `cratis-screenplay-render-and-gap-fill`.

## Lineage
Adapted in part from TrogonStack/agentskills (MIT); attribution and provenance entries are in
`references/provenance.md`; the notice is in `LICENSE`.
Read `references/provenance.md` when checking attribution, licenses or the origin of an adapted modeling practice.
