<!-- cratis-ai-managed: skills/cratis-application-slice-conformance/references/checklists.md -->
# Final checklists

Reconcile in both directions with each list. Moved here from the render workflow; it gives
the renderer no right to browse managed output or implement rejected semantics.

## Contract reconciliation
- Every modeled command field, event and payload field, rule, authorization gate, projection
  dependency, query behaviour and specification has a realization.
- Every business field, default, rule, filter or outcome in code has contract authority;
  otherwise stop and ask. Framework metadata is infrastructure and must not change the domain
  contract.
- Unrelated contracts stay as they were.
- Descriptions explain intent; they do not license contradictions or undocumented defaults.

## State Change
- Keep event-source identity and production destinations; add no duplicate identity payload
  unless the model needs it.
- Derive decision state from authoritative facts, no larger than the rules need; replay and
  decisions are deterministic and free of external effects.
- Name where each invariant is enforced atomically, including claims spanning sources. A stale
  view or a caller-supplied status is not enforcement; a state-dependent rule the model marks
  not enforced today needs a protected read or concurrency scope in code (where each rule
  belongs: `rules/vertical-slices.md`, "The decision matrix").
- Keep validation messages; separate validation rejection from authorization denial.
- Test the specified repeat behaviour: once-only occurrence, rejected retry, idempotent
  success and external-effect delivery are different guarantees.
- Verify command registration, transport mapping and inherited authorization against
  repository conventions, not a copied route template.
- No business rule in code that the contract does not state.
- Every property of the command has a field in the code's command and nothing else does: none
  invented, none missing. Every event keeps its modeled name and every payload field.
- Every contract specification maps to a spec. Explicit realization requirements bind
  hand-written delivery, cannot contradict executable parts and are never supplemented with
  inferred rules; descriptive prose that is not an explicit requirement is a hint and may become
  a validator message or code comment where it adds value, but never a new rule.
- No field name was assumed or guessed: if a field is not in the contract, it is not in the code.

## State View
- Keep field types, precision, optionality, identity and key selection; add no
  processing-time values or business defaults absent from the model.
- Every read-model property has a source, and the subscribed events equal the modeled
  events; do not subscribe to every nearby event. Honour the actual sources, joins, removals
  and auto-mapping.
- Keep population, later updates and removal; a partial update leaves other fields unchanged.
- Verify query cardinality, filters, caller scope and authorization; do not turn a list into
  a keyed lookup or the reverse.
- Verify registration and event consumption in the target runtime, and replay or rebuild
  behaviour; state any consistency limitation.
- No extra property, column or field beyond the read model the contract defines; every
  contracted property exists in the code's read model and, where one is persisted, in its
  storage mapping. No assumed event: the subscribed set is exactly the modeled one.
- One specification per contract specification, each seeding events and asserting the
  materialized state (`cratis-chronicle-read-model-specifications`).
- If the read model's projection looks up current state before changing it, the lookup is the
  projection's own mechanism, never a second connection or a side effect.

## Automation and Translation
- Each field of the produced command or event maps from the trigger event, an injected read
  model or a contract mapping, and from nothing else.
- No filter condition the contract does not state.
- The repeated-delivery behaviour in the contract has a specification: what happens when the
  trigger arrives twice.
- Every reaction in the contract has a realization matching its trigger. An event-triggered
  reaction is a Chronicle reactor whose handled events are exactly the trigger events the
  contract names (`cratis-chronicle-reactor`). A clock or application trigger is a scheduler or
  host signal that raises the occurrence, and a capture source (webhook, poll, topic) is an
  ingestion adapter that authenticates, parses and appends; both follow
  `cratis-engineering-effect-boundaries`, and each has its own boundary specification or
  integration check (`cratis-screenplay-automations-and-translations`
  `references/realization-and-gap-fill.md`).
- Conditions (skip when a field is absent, filter by state) come from the slice's description
  or specifications; no condition was invented.
- Exercise the reaction boundary itself: for a reactor, `ReactorScenario<TReactor>` invokes it
  with the trigger event and asserts what it produced (`ShouldHaveProduced<T>(predicate)`), which
  covers the field mapping from the trigger; the command it runs is specified separately with
  `CommandScenario`. Where the boundary cannot be reached in process, use an integration
  specification.
- A failed handling is not swallowed: failure behavior follows `cratis-chronicle-reactor`
  (failure and quarantine), never a catch that continues.
- **Real startup participation.** A `ReactorScenario` bypasses observer registration, so it
  cannot show that the application subscribes the reactor. Verify, through the repository's
  integration route or a startup check, that the real application discovers the intended
  reactor (its assembly is scanned, per the repository's discovery convention), that it observes
  the intended event store and event sequence, and that a trigger event delivered there reaches
  it. Report this apart from the in-process specifications.

## Chronicle runtime guarantees
Follow the owning corpus skill. Verify the actual namespace, subject, constraint scope,
guarded-read admission, migrations and replay. A passing generated specification suite proves
none of these. Unsupported semantics remain gap-fill scope, never hidden customizations.

## Evidence
- Map every accepted specification to its executed test or test family (name and qualified
  address kept; splitting is fine if all assertions survive).
- Passing all existing tests cannot show that missing denial, competing-claim, branch or
  removal cases are covered; check applicability upstream with `cratis-screenplay-scenario-coverage`.
- Use isolated fixture state; tests never depend on another test's writes.
- For storage, transaction, registration or migration claims, use the repository's integration
  route with real configuration. Report runner, configuration and per-specification outcomes
  apart from integration results; skipped and unexecuted cases stay visible.
- Record deliberate divergences and their approval; never call them equivalent behaviour.
