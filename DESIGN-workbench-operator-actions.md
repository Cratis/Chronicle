# Workbench Operator Actions — Safety, Ledger, and Verification

**Status:** proposed  
**Profile:** Chronicle framework  
**Pilot:** retry one failed observer partition  
**Related:** `PLAN-workbench-product.md`, `WORKBENCH-OPERATOR-ACTION-INVENTORY.md`,
`DESIGN-workbench-authorization.md`

---

## 1. Decision summary

Operator actions are Chronicle domain processes, not controller side effects.

The shared path is:

```text
caller
  → authenticate and authorize
  → optional explanatory preflight
  → durable action request
  → authoritative target-state evaluation and execution
  → Job correlation where applicable
  → verification
  → durable terminal outcome
```

Workbench, CLI, MCP, and automation use the same server contract. The frontend never supplies actor
identity and never becomes the enforcement boundary.

The failed-partition retry is the pilot because it is bounded to one observer partition, already has
server-side state checks, starts a durable Job, and has observable verification signals.

The pilot is action-specific. It does not begin with a generic coordinator, handler registry,
approval engine, or arbitrary action payload. Shared infrastructure is extracted only after a second
materially different action proves the common shape.

---

## 2. Goals

- Record who requested an action, why, what it targeted, and what happened.
- Derive eligibility from current server state.
- Prevent replayed HTTP requests from starting duplicate work.
- Return an Operation ID and any linked Job ID.
- Distinguish accepted, rejected, no-op, failed, cancelled, and verified outcomes.
- Verify effects rather than treating command acceptance as success.
- Preserve current public endpoints during an additive migration.
- Make the same behavior available to Workbench, CLI, MCP, and trusted automation.
- Keep target event-store and namespace isolation intact.

---

## 3. Non-goals

The pilot does not:

- generalize every existing command in one release;
- add replay-all or broad bulk recovery;
- automate recovery decisions;
- expose MCP mutations by default;
- store raw command bodies, event content, passwords, secrets, or stack traces in the ledger;
- replace Jobs;
- invent distributed transactions across an event append and a Job start.

---

## 4. Current path and gaps

Current retry path:

- `Source/Clients/Api/Observation/ObserverCommands.cs` exposes
  `TryRecoverFailedPartition()` and returns `Task`;
- `Source/Kernel/Core/Services/Observation/Observers.cs` calls the observer;
- `Source/Kernel/Core/Observation/Observer.Failing.cs` checks observer/failure state and starts or
  resumes `IRetryFailedPartition`;
- `StartRecoverJobForFailedPartition()` discards the returned `JobId`;
- Workbench executes the command directly from `FailedPartitionsViewModel.retry()`;
- `AllJobs` and `AllFailedPartitions` provide observable evidence, but they are not correlated to the
  initiating request.

Replay has the same product gap: the Kernel returns `ReplayResponse.JobId`, while
`ObserverCommands.Replay()` returns an empty HTTP result.

Redaction provides the closest native pattern. `EventSequences.Redact()` appends
`EventRedactionRequested` to the target namespace's System sequence with correlation, causation,
and actor context before a reactor performs the mutation.

---

## 5. Core concepts

Add the minimum concepts under `Source/Kernel/Concepts/OperatorActions/`:

- `OperatorActionId : ConceptAs<Guid>` — receipt identity and idempotency key;
- `OperatorActionExecutionStatus` — `Pending`, `Running`, `Rejected`, `Succeeded`, `Failed`,
  `InDoubt`, `Cancelled`;
- `OperatorVerificationStatus` — `NotStarted`, `Pending`, `Verified`, `StillFailing`,
  `Inconclusive`;
- `OperatorActionReasonCode` — `IncidentRecovery`, `Recommendation`, `ManualRemediation`,
  `Automation`, `LegacyUnspecified`;
- `OperatorActionReason` — reason code, optional bounded external reference, optional bounded note;
- `FailedPartitionRetryTarget` — event store, namespace, observer, and partition;
- `OperatorActionFailure` — stage, stable code, safe message, and retryability;
- `PreflightFingerprint : ConceptAs<string>`.

Do not add a generic action-kind registry in the pilot. The public service and facts already state
that this operation retries a failed partition.

Enum values are public wire contracts. Retired values are reserved and never reused.

### Reason data

A reason contains:

- required reason code;
- optional incident/ticket/reference, maximum 128 characters;
- optional note, maximum 512 characters;
- no passwords, tokens, connection strings, secrets, or copied event content.

Notes are retained only where the audit-retention policy permits them and are never emitted into
metrics or trace attributes.

---

## 6. Public service contract

Add an action-specific, additive failed-partition recovery service and committed protobuf package.
Do not publish a general `IOperatorActions` handler platform in the pilot.

Pilot operations:

```text
PreflightFailedPartitionRetry(request) → preflight response
StartFailedPartitionRetry(request) → receipt
GetFailedPartitionRetry(operation id) → details
ObserveFailedPartitionRetries(target) → observable summaries
RecordFailedPartitionRetryNoAction(request) → receipt
```

The start request contains:

- caller-generated `OperatorActionId`;
- structured reason;
- exact failed-partition target;
- optional expected preflight fingerprint;
- optional opaque investigation reference.

It does not contain actor identity.

The receipt contains:

- Operation ID;
- execution status;
- verification status;
- optional Job ID;
- whether this was a repeated request or joined execution;
- structured rejection/failure.

The same Operation ID with the same canonical request returns the existing operation. The same ID
with a different request returns `OperationIdConflict`.

---

## 7. Actor identity

The operation boundary requires an authenticated principal with a stable subject.

Do not reuse the random-GUID fallback in
`Source/Clients/Api/EventSequences/IdentityConverters.cs`.

The operation event relies on Chronicle event-context `CausedBy` identity:

1. resolve stable issuer + subject;
2. reject if stable identity is unavailable;
3. create the Chronicle identity context without accepting display details from the request;
4. persist/project the resulting `IdentityId`;
5. resolve display details separately for authorized readers.

This avoids duplicating email, name, and username across immutable audit events.

---

## 8. Authorization

Before preflight or execution:

- require the action permission;
- verify event-store and namespace scope;
- verify resource scope where configured;
- classify `Denied` and `Indeterminate` separately;
- fail closed.

The operation coordinator repeats authorization. An HTTP, gRPC, CLI, MCP, or in-process caller
cannot bypass the check.

See `DESIGN-workbench-authorization.md` for roles, permissions, internal-user migration, external
claims, CSRF, and authentication-disabled behavior.

---

## 9. Preflight and TOCTOU

Preflight is explanatory, not authoritative.

Failed-partition retry preflight returns only source-backed facts:

- whether the failure still exists;
- failure id and last failed sequence number;
- attempt count and last occurrence;
- observer and partition quarantine state;
- observer running/replay state;
- known conflicting retry/replay Job;
- eligible/rejected/insufficient-data decision;
- machine-readable reasons;
- fingerprint over the relevant state.

Execution re-evaluates inside the serialized observer grain turn. It never trusts the prior response.
If the supplied fingerprint is stale, return `PreflightStale` with current facts. Callers may refresh
and ask the operator again.

---

## 10. Atomic target operation

Add an observer method dedicated to operator recovery, conceptually:

```text
TryStartFailedPartitionRetry(operationId, partition, expectedFingerprint)
    → evaluated facts + outcome + JobId
```

Within one observer-grain turn it:

1. locates the current failure;
2. derives the current fingerprint;
3. rejects observer quarantine and conflicting replay;
4. treats partition quarantine as the reason automatic retries stopped, not as a prohibition on an
   explicit authorized retry; the preflight highlights it and requires the normal reason/confirmation;
5. rejects stale or otherwise invalid state explicitly;
6. starts, resumes, or joins the retry Job;
7. returns the Job ID and exact result.

Refactor `StartRecoverJobForFailedPartition()` to return its Job ID instead of discarding it.
Automatic retry remains supported through the existing internal path and does not require an
operator reason.

Extend the retry Job request additively with optional `OperatorActionId` for correlation only.
**Partition remains the Job deduplication key.** Two Operation IDs targeting the same active failed
partition must join the existing Job; using Operation ID in the Job predicate would permit concurrent
retry Jobs for one partition.

---

## 11. Durable ledger

### Placement

Target-scoped history events use a new reserved `OperatorActions` event sequence in the target event
store and namespace, with `OperatorActionId` as event source id.

Do **not** use `EventSequenceId.System`: it is public client API and client reactor definitions can
subscribe to it. A dedicated sequence preserves namespace isolation while allowing explicit server
enforcement:

- only Kernel-owned reactors can register against it;
- generic sequence discovery hides it;
- generic event query/export rejects it regardless of ordinary event permissions;
- append/revise/redact endpoints reject it;
- its event types/content are excluded from generic `chronicle.events.content.read`;
- only dedicated, sanitized action-history APIs protected by `chronicle.audit.read` expose its
  projection.

Genuinely cluster-global action history uses the protected sequence in `EventStoreName.System` and
`EventStoreNamespaceName.Default`. The sequence protection is part of Slice B, not a later hardening.

### Events

Initial past-tense, action-specific events:

- `FailedPartitionRetryRequested` — carries canonical request digest, target, reason, and optional
  investigation reference;
- `FailedPartitionRetryRejected`
- `FailedPartitionRetryStarted`
- `FailedPartitionRetryJobLinked`
- `FailedPartitionRetrySucceeded`
- `FailedPartitionRetryFailed`
- `FailedPartitionRetryBecameInDoubt`
- `FailedPartitionRetryVerificationCompleted`
- `FailedPartitionRetryNoActionRecorded`

Do not publish one generic event carrying arbitrary action payload. A second action family may reuse
concepts and lifecycle helpers while retaining self-describing event facts.

Event context carries occurrence, correlation, causation, and actor identity. Events never contain
their own Operation ID because it is the event source id. The request digest is persisted so a
repeated Operation ID can be distinguished from a conflicting payload.

### Query model

Project the events into a namespace-scoped `FailedPartitionRetryRecord` read model with:

- id;
- execution status;
- verification status;
- target;
- reason code/reference;
- actor `IdentityId`;
- requested/started/completed timestamps;
- Job ID;
- safe failure;
- verification evidence;
- investigation reference.

Use Chronicle's existing projection/read-model storage instead of adding one custom operator-action
storage implementation per backend. Events remain the authority; the read model provides query and observable UI access. Generalize the
read model only after a second action demonstrates truly shared query needs.

Before implementation, prove and document that action-history events:

- live on the protected OperatorActions sequence and cannot be consumed by client observers;
- are handled only by an action-specific Kernel reactor and cannot recursively create recommendations
  or more operator actions;
- remain queryable after the active failed partition disappears;
- follow the target namespace's documented retention/deletion lifecycle;
- cannot be appended, revised, redacted, exported, or queried through ordinary Workbench data
  operations; this requires explicit server enforcement because sequence identifiers are otherwise
  caller-controlled;
- are readable only with the action-history permission.

Call this a **durable operator-action history**. Do not claim tamper-proof compliance audit until
retention, deletion, access, export, and tamper-evidence guarantees are independently specified.

---

## 12. Idempotency and crash recovery

Version one uses `OperatorActionId` as the idempotency key.

Acceptance sequence:

1. authorize;
2. canonicalize request and calculate digest;
3. append `FailedPartitionRetryRequested` with a concurrency expectation that the Operation ID has
   no existing request;
4. on duplicate, compare the persisted digest;
5. return existing state for the same request or conflict for a different request;
6. invoke the atomic observer method;
7. append Job linkage and execution status.

There is an unavoidable crash window between request append and Job start. Close it with an Orleans
reconciler/coordinator grain keyed by Operation ID; Orleans single activation prevents concurrent
reconciliation on multiple silos. It finds non-terminal requested operations and re-invokes
execution. Observer/Job idempotency joins the partition's existing Job rather than duplicating it.

Activation is durable and redundant:

- an action-specific Kernel reactor observes `FailedPartitionRetryRequested` and activates the
  coordinator immediately;
- the coordinator registers an Orleans reminder while execution or verification is non-terminal;
- Kernel startup scans the action-history read model for non-terminal records and activates their
  coordinators;
- terminal events unregister the reminder.

This prevents a persisted request from remaining Pending forever after a crash or missed reactor
delivery.

Do not add a second idempotency database in the pilot. Add one only if measured lookup/throughput
requires it.

---

## 13. Verification

Retry acceptance is not recovery success.

Pilot verification requires per-partition evidence:

- linked Job completed successfully;
- the original failed-partition registration is no longer active;
- the same observer/partition did not register a replacement failure during a bounded stabilization
  window.

Do not use global observer `LastHandled` as recovery proof; other partitions can advance it beyond the
failed sequence while this partition remains failed.

Execution and verification are independent:

- execution `Succeeded` means the retry Job reached its successful terminal state;
- execution `Failed` means the effect is known not to have completed;
- execution `InDoubt` means dispatch/effect cannot be established safely;
- verification `Verified` means recovery evidence passed;
- verification `StillFailing` means the same target failed again or never cleared;
- verification `Inconclusive` means storage/observer evidence is unavailable or insufficient.

A succeeded execution may still have `StillFailing` or `Inconclusive` verification.

Persist evidence values and checked timestamps. Do not persist raw stack traces in the shared ledger;
the failed-partition attempt already owns that evidence.

The stabilization duration is configuration with a published default and must be approved before
implementation. Verification timing is durable—an Orleans reminder or durable verification Job—not
an in-memory timer, so silo restart does not lose the check.

---

## 14. Failure behavior

- Ledger request append failure: fail closed; do not start work.
- Authorization storage failure: `Indeterminate`; fail closed.
- Observer unavailable before Job start: execution is `Failed` when failure is known, otherwise
  `InDoubt`.
- Job may have started but durable effect/linkage cannot be established: execution is `InDoubt` and
  the reconciler queries/joins by Operation ID before any repeat dispatch.
- Verification evidence unavailable: `Inconclusive`, never success.
- Ledger projection delayed: direct Get may read operation events; list views may show declared
  projection staleness.
- Telemetry failure: must not invalidate an already durable ledger event or operation result.

---

## 15. API compatibility and migration

Do not change existing controller return types in place.

Add new operator-action endpoints returning Operation and Job identifiers. Migrate Workbench to the
new endpoint first.

Keep `TryRecoverFailedPartition` for one compatibility window:

- route and response remain unchanged;
- delegate to the shared internal execution path;
- create an Operation ID from the request correlation id;
- use `LegacyUnspecified` reason;
- record the authenticated actor where available;
- emit deprecation telemetry.

Legacy behavior when authentication is disabled must be an explicit owner decision and cannot remain
a permanent bypass.

Replay, observer quarantine clearing, Job controls, revision, and redaction migrate separately after
the pilot. Partition quarantine intentionally remains an automatic-retry state; explicit authorized
retry is its manual recovery path and does not require a separate un-quarantine action.

---

## 16. Workbench journey

1. Operator opens failed-partition details.
2. Workbench requests preflight.
3. Dialog shows exact observer, partition, attempts, quarantine, conflict, and unknowns.
4. Operator selects reason code and supplies optional reference/note.
5. Workbench generates Operation ID and submits.
6. UI navigates to operation details.
7. Operation links to the Job and exact failed partition.
8. Status advances through Running and Verification.
9. Success means verified recovery; inconclusive and recurrence remain visible.

No button is shown solely because the frontend thinks the state is eligible. Effective capabilities
and preflight are both required, and the server repeats all checks.

---

## 17. CLI and MCP

CLI and MCP eventually call the same failed-partition recovery service. A generic client contract is
introduced only after more than one action family exists.

CLI principles:

- JSON output by default;
- text is opt-in;
- unknown action/view values fail and list accepted values;
- structured errors contain stable code and remediation hint;
- read commands do not start a Kernel;
- mutation requires explicit flag plus reason.

MCP reads can ship after report parity exists. MCP mutations remain disabled by default and require a
separate approval design. No tool gets a privileged bypass.

---

## 18. Telemetry

Emit low-cardinality OpenTelemetry events/spans:

- `cratis.chronicle.operator_action.requested`
- `cratis.chronicle.operator_action.rejected`
- `cratis.chronicle.operator_action.started`
- `cratis.chronicle.operator_action.completed`
- `cratis.chronicle.operator_action.verification_completed`

Attributes: action kind, resource kind, event store, namespace, outcome, stable error code, permission,
Operation ID, Job ID, correlation ID, and duration. Do not put actor subject, reason text, partition
payload, stack trace, event content, or secrets in metric labels.

Metrics:

- action requests by kind/outcome;
- authorization denied/indeterminate counts;
- action and verification duration histograms;
- in-flight/non-terminal operations;
- verification recurrence/inconclusive counts.

---

## 19. Implementation slices

### Slice A — public concepts and decisions

- concepts/enums/reason validation;
- permission dependency;
- protobuf compatibility fixtures;
- owner approval for reason and stabilization policy.

### Slice B — action history and query model

- action-specific events in the protected target-namespace OperatorActions sequence;
- failed-partition retry projection/read model;
- Get/Observe contracts;
- namespace-isolation specs.

### Slice C — failed-partition atomic start

- observer preflight/evaluation;
- return Job ID;
- Operation ID on retry Job request;
- idempotent start/join specs.

### Slice D — failed-partition recovery service and reconciliation

- authorize;
- append durable request;
- execute;
- recover crash windows;
- structured outcomes.

### Slice E — verification

- Job/partition/progress evidence;
- stabilization window;
- terminal events and read model.

### Slice F — Workbench

- capability check;
- preflight/reason dialog;
- operation and Job progress;
- component/render and runtime SSE specs.

### Slice G — compatibility and runtime proof

- legacy adapter/deprecation;
- out-of-process authenticated scenario;
- concurrent same-ID request across silos;
- InMemory/MongoDB/SQL verification.

Each slice is one bounded branch/PR and passes Debug, Release, affected specifications, generated
proxy/protobuf checks, Workbench lint/tests/build, and documentation verification.

---

## 20. Rejected alternatives

- **HTTP audit middleware as the ledger:** cannot express semantic target, preflight, Job, or verified
  outcome and cannot share behavior with gRPC/in-process callers.
- **UI confirmation as safety:** bypassable and state can change after rendering.
- **Central System/Default ledger for target actions:** weakens namespace isolation.
- **Separate custom ledger storage as authority:** duplicates Chronicle's event-sourcing product and
  requires backend-specific implementations.
- **Client-supplied actor:** forgeable.
- **Random subject fallback:** unstable across retries.
- **Client-only idempotency:** duplicate network requests still start duplicate work.
- **Command returned 200 as success:** no behavioral verification.
- **Generic action framework before the pilot:** hides action semantics and expands scope before the
  common shape is proven.
- **One giant migration of all actions:** too much blast radius for the reliability program.

---

## 21. Owner decisions

- Reason-code taxonomy and bounded-note policy.
- Stabilization window default.
- Legacy endpoint compatibility duration.
- Production behavior when authentication is disabled.
- Whether service principals may execute human operator actions.
- Operator action and completed Job retention.
- Whether MCP may ever mutate.
- Cross-store audit export policy.
