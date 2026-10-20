---
id: 0014-projection-generations
title: Give projections and reducers durable generations, publish one Active generation per materialization, and resolve reads explicitly
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-20
decider: woksin
applies-to:
  - Source/Kernel/Core/Projections/**
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/ReadModels/**
  - Source/Kernel/Storage*/**
  - Source/Clients/**
  - Documentation/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Give projections and reducers durable generations, publish one Active generation per materialization, and resolve reads explicitly

Accepted by woksin on 2026-10-20. This is the product contract, not an implemented generation capability. [Decision 0016](0016-materialization-binding-ownership-and-fencing.md) defines publication and provider proof; [decision 0017](0017-generation-enablement-compatibility-and-recovery.md) defines enablement and recovery. [Decision 0015](0015-global-projection-scope-is-restricted.md) remains the immediate global-scope restriction.

## Context

### Invariants and non-goals

- Exactly one durable binding selects Active for a materialization. Registration, software-version order, cached state and catalog lifecycle labels do not select it.
- Rebuilds write isolated, immutable physical destinations while Active continues following live events. A candidate never leaks partially rebuilt data into ordinary reads.
- Publication and rollback use one fenced, recoverable protocol. Unknown ownership, coverage or authority fails closed; neither a force option nor a lost response permits guessing.
- A query uses one generation and its schema throughout resolution, data access and compliance release. This is generation consistency, not snapshot isolation of concurrent writes or an atomic bundle of read models.
- Erasure and source-history validity dominate read availability and rollback. Pins cannot authorize stale or erased data.
- No rename/view authority, forced direct-reader conversion, general historical binary loader, universal hermetic runtime, distributed multi-sink transaction or arbitrary cross-partition parallelism is promised. Shared-global materializations, staged side-effect publication and snapshot/bundle capabilities need separate proof.

### Current implementation, not generation guarantees

The source baseline is `fb5968a46ab962af4c50bfd0c1387b933d04aec1`:

| Existing behavior | Boundary of that evidence |
| --- | --- |
| [Automatic definition evolution disables per-event-source historical replay and selects full replay or a recommendation according to policy; globals receive a recommendation](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Projections/Projection.cs#L595-L624). | A conservative safety path, not partial generation publication. Explicit partition replay remains separate. |
| [Projection pipelines compute a canonical runtime fingerprint](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Projections/Engine/ProjectionFactory.cs#L126-L136); [normalization removes semantic fields recursively](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Projections/Engine/ProjectionDefinitionNormalization.cs#L28-L40). | Restoring top-level model/state does not establish complete recursive semantic identity or a durable revision catalog. |
| [The .NET reducer SDK supplies an implementation Hash](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Clients/DotNET/Reducers/Reducers.cs#L483-L499), [compared by the kernel](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Observation/Reducers/Clients/ReducerDefinitionEvolution.cs#L47-L53). | [Referenced members contribute identity](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Clients/DotNET/Reducers/ReducerILFingerprint.cs#L65-L76), not necessarily changed helper bodies or external dependencies. |
| [Keyed materialized reads use FindOrDefault and latest-schema compliance release](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/ReadModels/MaterializedReadModelStore.cs#L37-L55). SQL [key reads follow the active replay table](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.Sql/Sinks/Sink.cs#L137-L151), while [paged queries default to the primary table](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.Sql/Sinks/Sink.cs#L488-L499). | A page-only isolation test cannot establish keyed serving isolation or generation-correct schema release. |
| [SQL index creation is implemented, with unsupported paths skipped and creation failures logged](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.Sql/Sinks/Sink.Indexes.cs#L12-L49), including [replay begin/resume setup](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.Sql/Sinks/Sink.cs#L314-L337). | Successful legacy sink setup is not proof that every required index exists on a generation's container. |
| Legacy [SQL replay attempts its swap after bulk flush and returns known failures afterward](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.Sql/Sinks/Sink.cs#L342-L354); [MongoDB distinguishes known failed partitions from a throwing/unknown final flush](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.MongoDB/Sinks/Sink.cs#L749-L769). | Legacy finalization is not the generation coverage/publication proof selected here. |

## Decision

Give projections and reducers immutable revisions and independently materialized generations, publish one Active generation through an authoritative binding, and resolve reads explicitly against that binding or a retained generation pin. Keep the feature opt-in and default off; enable only complete, proven capability slices under decisions 0016 and 0017, without changing legacy contracts for event stores that have never been initialized for generations. An initialized store stays binding-governed even if the flag is later turned off (decision 0017).

### Identity and execution isolation

| Identity | Meaning |
| --- | --- |
| Definition revision | Immutable semantic declaration, independent of software version and materialization. |
| Execution specification | Durable bounded inputs for executing/interpreting a revision: complete recursive definition/child initial state, target schema, relevant event-schema/delivery references, resolved semantic sink configuration, reducer artifact identity, engine compatibility, canonicalization version and declared semantic dependency/configuration revisions. |
| Generation | Independently materialized lineage for a logical identity, materialization scope and sink; repair may create another generation at the same revision. |
| Attempt | Construction/retry identity within a generation's permitted build protocol; never an excuse to reuse an abandoned lineage. |
| Serving epoch | Monotonic publication identity used with expected generation and ownership fences; not a definition version. |
| Store incarnation | Identity renewed after restore, excluding pre-restore operations even if numeric epochs repeat. |

Exclude connection IDs, promotion policy and operational activation from semantic identity. Never hash credentials; version semantic configuration without disclosing secrets. Canonicalization has versioned golden vectors, preserves semantic array ordering and recursively includes relevant inputs. Persist immutable snapshots, verify hash equality against canonical content, reserve identifiers/names atomically and reject collisions rather than truncate or adopt unrelated data.

The .NET SDK/build supplies reducer artifact identity plus declared dependency/configuration revisions and an explicit semantic-revision escape hatch. Identity does not prove deterministic use of time, randomness or services. Applications supply exact compatible old/new reducer hosts and capacity; Chronicle does not load arbitrary historical binaries. No compatible host means waiting/unavailable, never dispatch to newest. A rebuild starts with empty reducer state. Correct per-key retry/mutation receipts must handle overlapping and recut batches: the [current watermark guard explicitly permits a straddling resumed batch to reapply its prefix](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Observation/Reducers/ReducerPipeline.cs#L125-L143).

Adopt generation 1's existing observer ID. Later generations have durably reserved, collision-checked bindings, not identities inferred by parsing a user ID suffix. Every generation has independent positions, failures, jobs, locks and explicitly addressed sinks. Isolation covers pipeline/sink caches, replay contexts, reducer dispatch/current-definition refresh, changeset history, futures/pending saves and cleanup. Internal children and event-driven joins stay in the parent generation. Genuine external materialization dependencies require explicit dependency bindings/consistent-cut semantics or capability refusal; ordinary joins do not mandate cross-model bundles.

Persist immutable physical names once. Adopt existing collections/tables in place; new generations use provider-safe names derived from the complete owner tuple with a bounded prefix/hash and generation suffix. Respect provider equality/length/quoting rules and reserve final names. Multiple producers of a logical container require an explicit shared owner or fail admission. Promotion never renames storage or changes database placement. Only one sink is authoritative per served materialization.

### Evolution and provenance

| Change class | Required action |
| --- | --- |
| Operational metadata only | No semantic evolution. |
| Proven no-work semantic change | New immutable revision in the same generation, with explicit applicability evidence. |
| Proven partial evolution | Pending revision with isolated per-partition shadow/adoption receipts before served adoption. |
| Unknown/unsafe, key/scope/destination change, incompatible execution/schema, or repair | New isolated generation. |

Until partial proof exists, use policy-preserving full rebuild/recommendation, never historical partial writes into live state. Keep Automatic, PartialOnly and Manual build-admission policy: Automatic starts an isolated full build; PartialOnly/Manual records the desired revision and recommendation without starting a full job. Automatic promotion follows proven readiness by default. Operator, deployment or scheduled approval is a separate explicit opt-in, not a redefinition of Manual admission.

Historical fingerprint registration reuses revision identity; it neither chooses a generation nor authorizes cancellation, rollback or publication. Desired-definition changes are explicit intent, not old-replica reconnects. Returning deliberately to the applied revision abandons its superseded candidate without allocating another build.

| Provenance field | Required interpretation |
| --- | --- |
| Desired revision | Explicitly requested future execution; may be unbuilt. |
| Applied revision | Revision whose evolution has been applied to a particular generation, with adoption evidence. |
| Served revision/schema | Binding-selected interpretation for that materialization; not latest registration/schema. |
| Producing revision and source history | Known execution/history evidence, or explicitly unknown legacy provenance. A current definition alone does not prove what produced existing data. |
| Legacy occurrence | Archived, non-following and unknown unless evidence proves its producing revision/schema/coverage; never invented as a rollback target. |

In-place revision adoption does not retain the earlier revision's data snapshot. A software-version registration can resolve to several repair generations; explicit bindings must disambiguate or fail.

### Lifecycle and authority

| State | Meaning | Allowed next states |
| --- | --- | --- |
| Building | Isolated historical construction, possibly unstarted/waiting; never default-served. | Following, Abandoned |
| Following | Historical coverage proven; ongoing live coverage obligation with current health metadata. | Active, Abandoned |
| Active | The generation selected by the authoritative binding; follows live events. | Retired |
| Retired | Displaced, still following, bounded rollback target; not the generic stopped-observer flag. | Active, Archived |
| Archived | Non-following retained snapshot with last proven coverage or explicit unknown provenance. | Removed |
| Abandoned | Unserved canceled, superseded or terminally failed build, drained before cleanup. | Removed |
| Removed | Physical data removed; immutable audit/identity tombstones retained. | None |

Active is derived from the binding and publication receipt; catalog labels reconcile from it, never form another serving authority. Pair promotion/rollback with displacement of the previous Active. Adoption alone imports legacy Active. Separate running/paused state, health, admission, handover phase and deadlines from lifecycle. Retriable failure remains Building; retrying Abandoned/Removed allocates a new generation. Historical Building delivery uses replay semantics; Following/Active/Retired use live semantics. Prior readiness does not prove current Following coverage.

### Reads, pins, observables and application rollout

| Read role | Contract |
| --- | --- |
| Ordinary read | Resolve Active once per operation with a retention lease; use its generation/schema through keyed/paged reads, joins, immediate/session reconstruction, reducer reads and compliance release. Pipeline state reads use their execution generation instead. |
| Explicit pin/selector | Resolve a specific generation and interpretation with lifecycle/coverage metadata. Software version alone is not identity. Reject ambiguous, unready, incompatible, expired or removed selection; no silent Active fallback. Building is diagnostic-only with incomplete-state acknowledgment. |
| Retention lease | Bounded and renewable, prevents deletion while valid; not an indefinitely held database transaction or a snapshot of concurrent writes. Existing pins stay on their original generation after repair, promotion or rollback. |
| Observable | Watch the resolved physical generation. Default subscriptions re-resolve on publication, invalidate caches, rebind streams and emit one logical full reset/reload without an intermediate empty result. Pinned subscriptions stay pinned; reconnect/missed notifications recheck authority. |
| Application rollout | Use consumer-compatible schema, coordinated deployment, or explicit generation pins. Supporting all three does not imply an old client can deserialize arbitrary new schemas. |

An operation overlapping publication may use either complete generation, never a mixture. A read starting after completed publication must resolve the new authority, not an indefinitely stale cache. Generation consistency is the default; true snapshots and atomic cross-model bundles are separate capabilities. Archived reads expose a retained snapshot's known limits, not current freshness. Direct readers of the original physical name remain on generation 1; opt in to binding-aware Chronicle reads/adapters to follow Active. No stable-name Active alias is promised.

### Retention, repair and source-history safety

Default enabled retention is one following Retired generation for 24 hours and one Archived snapshot; kernel-owned models retain neither by default. Preserve ReplayedVersionsToKeep as the snapshot-count compatibility alias. Provide quotas/opt-outs for follower count, storage, lease duration and exact reducer-host capacity. Never evict Active or pinned data silently to meet quota; reject/queue work and expose pressure. Each successful displacement starts a fresh rollback deadline; failed attempts do not extend it. Expiry/cap enforcement drains before archiving; a zero window schedules archival, not a fictional follower.

Rollback uses the same publication protocol to a currently proven Retired target. Archived/unknown occurrences need a new catch-up/rebuild lineage. Force may waive only a defined failed-partition policy, never coverage, ownership, drain, membership, history validity or interpretation compatibility. Retention pins delay physical removal, not erasure enforcement.

Once enabled, legacy full Replay admits a new repair generation at the Active execution revision selected at admission, not a pending desired-definition candidate. Publish it through the same binding protocol, preserve JobId/NotSet and existing job completion meaning, and complete only after repair is served; do not insert evolution-approval waiting. Original generation pins remain where they were. Explicit partition replay needs shadow/adoption proof or clear refusal; never-initialized stores keep legacy behavior except independent safety fixes. Decision 0017 specifies concurrency and recovery.

Coverage includes a source-history revision/invalidation fence, not only a sequence frontier. Revision/redaction below that frontier invalidates or repairs every affected readable/following generation, including in-flight builds. Erasure dominates rollback and read availability: repair affected generations or make them unreadable/delete them according to policy; fail closed until compliant. Neither slow publication, retained snapshots, restore nor pinned access may resurrect erased data.

### Capability and performance boundary

Require end-to-end provider/sink capabilities before allocation or external mutation: isolated destinations, execution identity, binding-aware reads/schema, coverage, mutation receipts, storage-enforced data/DDL/cleanup fencing, restore reconciliation and required indexes per container. EventSequenceSink appends visible facts and is rejected for shadow builds absent separately proven staged publication. The same observable contract applies to in-memory providers; otherwise refuse support. SQL's existing best-effort index operation is not generation readiness evidence.

Admit complete vertical capabilities, not a provider name or partial reducer claim. First-minor scope may differ by provider; legacy reducers/providers continue supported when their generation capability is refused. Measure fenced batch throughput/latency, conflicts/aborts, handover duration, lease cost, follower write/storage amplification and compatible reducer compute capacity against declared operational budgets before advertising support. Bounded batching is allowed; no mandatory per-event transaction/general outbox is introduced. Numeric budgets and mechanisms require executable proof, not acceptance by inference.

### Maintainer choices settled on 2026-10-20

Every choice below is accepted by woksin; remaining uncertainty concerns proof, not permission to select another product contract.

| Choice | Accepted position |
| --- | --- |
| Unreachable writer | Require storage-enforced takeover long-term; fail closed until proven. |
| MongoDB topology | Require transaction-capable topology initially; retain disabled legacy mode elsewhere. |
| Old binaries | Bridge release plus documented enforceable exclusion; bounded stop/start allowed where unavoidable, never read-model copying/renaming. |
| Query consistency | Generation consistency by default; snapshots/bundles are separate capabilities. |
| Enabled legacy Replay | New repair generation at admitted Active revision, preserving job completion meaning. |
| Historical reducer execution | Application-provided exact compatible hosts and explicit capacity/waiting, no general binary loader. |
| First-minor coverage | Complete tested slices; do not advertise reducer parity or an unproven provider. |
| Consumer schema compatibility | Support compatible schema, coordinated deployments and explicit pins; arbitrary old-client deserialization is not promised. |
| Retention cost | Keep defaults, with quotas/opt-outs and documented reducer compute cost. |
| Erasure | Erasure dominates reads/rollback; fail closed until affected generations are compliant. |

## Options considered

- **Isolated generations plus one binding (chosen):** independent live/build execution and auditable rollback, at storage/coordination cost.
- **Dual writing through one observer, newest version or last registration wins:** cannot express independent coverage or operator intent.
- **Every semantic change rebuilds:** safe fallback, but proven no-work/partial changes may reuse lineage without losing immutable revision history.
- **Rename/view publication or another pointer alongside a stable alias:** introduces moving destinations or competing authorities; not the selected contract.
- **Archived snapshots as instant rollback:** unknown/stale coverage cannot meet current publication proof.

## Default if unanswered

Acceptance settles the product choices, not runtime readiness. Keep generations disabled wherever the protocol, capability floor or provider proof is incomplete. Continue conservative legacy safety paths without advertising uninterrupted generation rebuilds, version-selected reads or instant rollback.

## Timeline and scope

Ship generations as an additive, default-off minor capability with unchanged APIs, version ordering, replay completion and reads for event stores that have never been initialized for generations. Default-on behavior, mandatory approval or forced reader conversion waits for a future major. Basic binding/pin safety, exact reducer execution when advertised, provenance and downgrade safety are not deferrable. Partial evolution, advanced selectors/gates and acceleration may follow after individual proof. Reactor version changes retain single delivery ownership/stable delivery identity and at-least-once retries, not dual following-generation execution or exactly-once external effects.

Implementation sequencing and release tracking belong to [epic #5155](https://github.com/Cratis/Chronicle/issues/5155). Shared-global materialization stays restricted under decision 0015 until coordinated ownership, deterministic input semantics and provider exclusion are proven.

## Verification

**Done when:** complete supported slices implement these invariants and decisions 0016/0017 with executable failure, compatibility and performance evidence. Acceptance alone does not advance stage beyond none.

**Verify by:** state-machine tests; golden recursive fingerprints and collision tests; nonempty independent-oracle live/build/rollback tests; exact reducer dispatch and recut retries; source correction/erasure non-resurrection; keyed/paged/immediate/session/schema/observable/pin races; quota/expiry tests; released .NET and generated TypeScript/Kotlin/Elixir/Python compatibility; every advertised provider/topology's failure-injected fencing and restore tests. Check flag-off behavior independently. Position distance or empty-history results alone never prove coverage.

## Consequences

Live serving, revision intent and rebuild progress become independent and auditable. Followers consume storage, writes and exact reducer capacity; explicit budgets bound that cost. Binding-aware readers gain generation consistency without forced physical conversion. Provider fencing, compatibility exclusion, history/compliance and recovery remain blocking implementation proofs, not assumed properties of Orleans activation or metadata CAS.
