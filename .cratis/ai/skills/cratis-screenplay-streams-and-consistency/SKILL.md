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

**First append into a scope is not checked by default.** The optimistic strategy compares the scope's tail sequence number; with no matching event yet there is no tail, so two writers opening the same source can both succeed. A concurrency scope alone does not protect "only one writer may open this": enable `ConcurrencyOptions.CheckFirstAppendIntoAScope` or `ExpectingNoMatchingEvent()`, or use a unique event constraint. Treat the default as unchecked until target evidence shows otherwise. Read [references/consistency-and-concurrency.md](references/consistency-and-concurrency.md) ("First append into a scope") for the options and the evidence to require.

## Product sources

Read [references/product-sources.md](references/product-sources.md) when checking a rule against the Screenplay, Chronicle, Arc or Stage sources.

## When / when not
- Use: choosing `identifier` and `for`; adding or naming a `constraint`; a rule that depends on
  earlier state; "can two people do this at once?"; retries and duplicate requests; splitting or
  merging streams, events or modules; adding, renaming or re-meaning an event; who owns which
  interface; reviewing collection- or log-shaped streams.
- Not for: projection and query syntax, reactions and captures, writing the specifications
  themselves (`cratis-screenplay-scenario-coverage`), Chronicle C# mechanics
  (`cratis-chronicle-event-modeling`). See Route near misses.
- Apply model-first per `cratis-screenplay-modeling-lifecycle`; never weaken
  authorization, `pii` or rules to compile.

## Interview phase
Read [interview prompts](references/interview-batch.md) when identity, competition,
retry, ownership, growth or stored-event history is unsettled. Ask only missing
questions, one at a time; ownership options are in `references/boundaries.md`.
Unattended work takes the conservative option and marks it `ASSUMED` in model
`documentation` and `.ai-work/screenplay/<model-slug>/STATE.md`.

## Procedure
1. **Establish identity and membership.** For every `concept XId` used as an `identifier`, write
   one sentence: "one business identity per stream: one stream per <business thing>; it answers
   <question>". Run the event-by-event check in `references/stream-patterns.md`: inspect every
   producer and destination that can write its history (commands, reactions, captures), say why
   each event belongs, and decide retain, split, view instead, or unresolved, by lifecycle and
   invariant consequences, not names or length. For Chronicle keep event-source identity,
   namespace, process-stream routing and concurrency scope apart
   (`references/chronicle-boundaries.md`). Declared `eventsource`/`stream`/`streamId` and
   command/specification routes bind and reference-execute as ESM v8.
   The CLI (toolchain `references/renderable-subset.md`) reaches Stage construct checks through its bundled ceiling; keep the routing
   contract when target admission requires gap-fill. Forms/boundaries:
   toolchain `references/sources-and-streams.md`. A `generated identifier` (ESM v7; Stage renders supported UUID/String strategies and v15 freshness; unsupported types/results still refuse -028/-038) is
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
     decision; that is not "read models never drive decisions". See
     `../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md`.
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
5. **Plan evolution**: read `references/evolution.md` before editing stored contracts
   for change classification, compatibility scenarios and enum-number safety.
   After release-baseline admission, revisions need `newGeneration`. Identity-affecting edits belong to the
   identity owner (`cratis-screenplay-modeling-lifecycle`).
6. **Test ownership and interfaces** with `references/boundaries.md`: ownership interview,
   interface and processor inventory, one real interaction walked across each boundary, coupling
   check. Keep team, module, stream and deployment boundaries distinct; inbound facts via
   Translate; unresolved decisions stay visible.
   Read [references/multi-service-models.md](references/multi-service-models.md) when the system spans several services, each with its own event store and model folder.
7. **Record and gate.** Durable rationale in feature and slice `documentation`; open consistency
   decisions and capability gaps in `STATE.md`. Then the gate below.

## Rules
**Compiler contracts** (diagnostics; capability per tool in the toolchain `references/executable-subset.md`)
- `identifier` on an event property is PLAY0019; the source id is never payload by declaration.
  Copying it as a payload value is PLAY0469 (information, warning for inline events);
  for a possibly stored contract, retire it as an evolution draft with `propose-property`
  `remove` + `newGeneration: true`, not by editing the event in place (model-authoring [property planning](../cratis-screenplay-model-authoring/references/property-links.md#remove-from-an-inline-event)).
- In the executable model a production's `for` must resolve to the command's scalar identifier:
  one command writes one stream declaratively; described handlers bind v23 but
  their payload-only contract does not grant arbitrary destinations.
- Constraint names are application-wide identities (PLAY0392); properties must be declared
  directly on the event (PLAY0391); never mix event and property rules under one name (PLAY0135);
  `ignore casing` is not allowed on `unique event` (PLAY0393).
- Generations are declared in full, numbered 1..N without gaps, in the same slice (PLAY0446-0448); `id` pins are shared by all generations and unique application-wide (PLAY0472); `storedName` needs ESM v16. See `references/evolution.md` ("Generations, pins and storedName diagnostics").

- Application-root `command reads protected` admits eligible reads and single-level
  requirement/mapping paths (v25). Absence rejects, matching decide/commit changes
  conflict. Legacy reads remain PLAY0271/PLAY0268; `concurrency` remains unbound and
  combining it with protected reads is PLAY0783. Read
  [protected reads](../cratis-screenplay-command-surface/references/protected-reads.md)
  for admission and protected-read rendering (Stage 4.138.0; STAGE-ESM-044 otherwise; the v25 pair itself is admitted). `description` text never reaches rendered code: a rule that lives only in prose is
  unenforced there.
- `pii` and `secret` bind as ESM v13;
  enum/identity compliance and protected events with explicit subject marks still
  refuse PLAY0268 (command-surface `references/compliance.md`). Legacy markers are
  deprecated (PLAY0565). Never remove protection to pass a tool. PLAY0515 also refuses
  scalar/composite stream-id types and route mapping sources, including nested paths.
- Stream-id literals must be nonempty NFC text; composite ids use at least two named scalar parts; PLAY0504 is an error for route/destination type mismatch; a stored stream name exactly `All` (and a stored source name exactly `Default`) is refused with PLAY0802, including via an `id` pin (rename via `propose-rename`; a pin away from `All` is valid). Read [references/stream-id-rules.md](references/stream-id-rules.md) when choosing stream-id types, literals, composite ids or renaming a stream.

- Chronicle limits: before 19.59.6 (InMemory: 19.59.3) the SQL and InMemory providers do not settle unique claims (see the Requires line below);
  index updates run after commit and may fail silently; composite values can
  collide on the separator. A declared constraint is a declaration, not a runtime test.
- `[ProtectedDecision]` ships in Arc and Stage's rendered package set. It is not emitted protection: gap-fill must enroll an admitted
  decision read, or redesign the stream/identity when the shape is refused
  (`references/consistency-and-concurrency.md`, section 3).

Requires Screenplay 4.150.0 or later (`screenplay --version`) for the `All` stored-stream refusal, 4.152.0 or later for `storedName` (ESM v16) and 4.153.0 or later for the release baseline.

Requires Arc 22.39.0 or later and Chronicle 19.23.0 or later for `[ProtectedDecision]`.

Requires Chronicle 19.59.6 or later (19.59.3 for the InMemory provider) for the SQL provider to settle unique-constraint claims; before it, two concurrent appends can both claim one value.

**Modeling defaults** (deviate with a recorded reason)
- One business identity per stream; cross-stream references are payload properties.
- A read model's key is not a stream identity. Read [references/read-model-keys.md](references/read-model-keys.md)
  when choosing one id, flat composite key parts (v27) or a composite-typed key.
- Copy the source id into the payload only when a consumer needs it as a value.
- Give each invariant an authoritative atomic decision point and inspect every path that could
  violate it. A constraint violation rejects that command's whole append set; a reaction cascade
  is not one transaction. Declared semantics are not a passed concurrency test.
- New models declare `reaction batches atomic`: direct productions per trigger
  occurrence and each selected refusal branch are separate batches; invoked commands
  stay independent. Legacy absence retains per-fact appends. Stage refuses v22
  with STAGE-ESM-016 and atomic mode in admitted v23–v27 with STAGE-ESM-043. Read
  [atomic boundaries](../cratis-screenplay-captures-and-reactions/references/atomic-batches-and-source-mappings.md)
  when defining failure rollback; batching is not redelivery deduplication.
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

**Review questions** (findings only with a domain consequence): read [references/review-questions.md](references/review-questions.md) for the stream, race, optional-property, boundary and eventual-consistency questions (decision tree and red flags for collection- or log-shaped streams are in `references/stream-patterns.md`).

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
Read [references/neighbouring-skills.md](references/neighbouring-skills.md) when the work is grammar, Chronicle C# compliance, reactions, slice design, review or rendering rather than stream and consistency decisions.

## Lineage
Adapted in part from TrogonStack/agentskills (MIT); attribution and provenance entries are in
`references/provenance.md`; the notice is in `LICENSE`.
Read `references/provenance.md` when checking attribution, licenses or the origin of an adapted modeling practice.
