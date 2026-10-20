---
id: 0017-generation-enablement-compatibility-and-recovery
title: Enable generations through an enforceable protocol floor, adopt legacy data in place, and recover under a new incarnation
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-20
decider: woksin
applies-to:
  - Source/Kernel/Core/Configuration/**
  - Source/Kernel/Core/Patching/**
  - Source/Kernel/Core/Projections/**
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/ReadModels/**
  - Source/Kernel/Storage*/**
  - Source/Clients/**
  - Documentation/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Enable generations through an enforceable protocol floor, adopt legacy data in place, and recover under a new incarnation

Accepted by woksin on 2026-10-20; no generation initializer, floor or recovery protocol is claimed implemented. This implements the compatibility boundary of [decision 0014](0014-projection-generations.md) and depends on [decision 0016](0016-materialization-binding-ownership-and-fencing.md)'s provider proof. [Decision 0015](0015-global-projection-scope-is-restricted.md) is a separate bounded restriction, not this rollout fence.

## Context

Additive metadata cannot make an old kernel honor new ownership. At the current source baseline, [MongoDB definition saves replace the definition record](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.MongoDB/Projections/ProjectionDefinitionsStorage.cs#L46-L59), and [replay finalization claims/renames physical collections](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Storage.MongoDB/Sinks/SinkCollections.cs#L98-L132). Neither path checks a generation floor. A new membership observer cannot retroactively fence those commands.

Installation and later enablement are different events. [PatchManager calls Up before saving its receipt](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Patching/PatchManager.cs#L69-L86); adoption must survive that crash window, and a startup patch alone cannot initialize an event store enabled later.

The released [.NET Replay signatures return JobId, including NotSet for the documented unavailable cases](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Clients/DotNET/Projections/IProjections.cs#L137-L150). The [key-read request's fields 1–10 have no generation selector](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Contracts/ReadModels/GetInstanceByKeyRequest.cs#L15-L70). Compatibility therefore needs explicit enabled repair semantics and additive read selection, not silently changed wire identities.

## Decision

Ship generations as a default-off additive capability behind a bridge release and durable per-store protocol floor, enable only after documented enforceable exclusion of incompatible binaries and legacy work, and idempotently adopt existing physical data as generation 1 in place. Preserve released contracts, use new repair generations for enabled legacy full Replay, refuse unsafe downgrade, and recover restored stores only after reconciliation under a new incarnation. A bounded stop/start transition is allowed where exclusion requires it; read-model copying, renaming or conversion is never the enablement mechanism.

### Bridge release and exclusion

Use a kernel Features.ProjectionGenerations flag (proposed name), default false. A bridge release must understand the floor, check admission before any generation-sensitive registration/read/write/job/cleanup mutation, and refuse mutation when it cannot satisfy an initialized store's required protocol capability. Keep the generation flag off during the preparatory rolling upgrade. Additive schema/metadata installation alone performs no adoption, generation allocation, routing change or generation writes.

Before initializing a store, establish the compatible kernel/worker/administrative capability set, exclude incompatible binaries, and drain outstanding legacy commands, replay sessions and jobs. Membership/version checks are necessary diagnostics, not exclusion: an unaware binary or already authorized storage session can ignore them. Document and prove the actual deployment/access barrier for each provider, including revoking incompatible principals/sessions, preventing reconnect/restart and exhausting or excluding delayed commands. A bounded stop/start is permitted when unavoidable; do not claim uninterrupted enablement without evidence.

The floor and initialization marker are durable and fail-closed. No legacy mutation path can remain reachable merely because one new coordinator owns the grain. Decision 0016's data, DDL and cleanup exclusion must hold before adoption/transfer. A procedure that cannot enforce old-binary exclusion refuses enablement rather than relying on operator intent or a flag. Application-version coexistence is distinct from kernel protocol compatibility.

### Explicit, retry-safe enablement initializer

| Step | Required evidence/result |
| --- | --- |
| Validate request | Explicit store enablement, supported provider/topology, proven floor/exclusion, binding-aware reader adoption, capacity and no unresolved legacy jobs/sessions. Drain or explicitly repair incomplete legacy replay; never guess its generation. |
| Install/recheck additive metadata | Idempotent tables/collections and unique identity/name reservations; recheck on later enablement even when startup patches already ran. |
| Adopt legacy Active | Bind existing physical container, observer ID, positions and failed obligations as generation 1 without copying, renaming, changing placement or losing indexes. The existing name remains a physical generation-1 name forever. |
| Record provenance | Preserve the current declaration separately from proven producing revision/schema/history. Use an explicit legacy-served/unknown-provenance binding where production evidence is absent; do not fabricate it from the current definition. |
| Import historical occurrences | Archived, non-following, with unknown production/coverage unless evidence proves otherwise; not fresh rollback targets. Preserve audit, identifiers and schema history. |
| Commit/reconcile initialization | Publish compatible binding/floor/receipts with durable recovery state. Re-entry after every partial step produces the same reservations/binding, never another generation 1 or duplicate adoption. Admit runtime work only after complete reconciliation. |

Stage adoption and runtime admission so a crash cannot expose half-initialized routing. Prove the control/data/receipt ordering and recover it; do not assume a patch receipt or single activation makes it atomic. Unknown served provenance limits revision-dependent selectors, rebuild equivalence and rollback claims until a verified build establishes them. Existing supported legacy reads may use the explicitly marked legacy-served binding, subject to compliance safety.

Direct readers of the old physical name remain on generation 1. Applications that need Active must explicitly adopt binding-aware Chronicle reads or a coordinated adapter; deliberate generation-1 access is allowed with its stated limits. Do not convert a collection into a view, rename a SQL table or force existing readers onto a new destination. After promotion, that original name is not an Active alias.

### Disabled/enabled API compatibility and repair

For an event store that has never been initialized for generations, with the flag absent/off, retain supported legacy registration/version ordering, containers, reads, replay policy/jobs and completion semantics, except independently released correctness fixes. Legacy semantics apply only to such never-initialized stores; an initialized store stays binding-governed regardless of the flag (see Downgrade and restore). Preserve the [existing Automatic evolution default](https://github.com/Cratis/Chronicle/blob/fb5968a46ab962af4c50bfd0c1387b933d04aec1/Source/Kernel/Core/Configuration/Observers.cs#L80-L83). Enabled default promotion is automatic after readiness; approval remains a separate opt-in for evolution, not a new mandatory replay state.

Keep released method signatures, RPC identities, request field numbers/meaning and JobId/NotSet behavior. Add commands/selectors through additive contracts and separate optional capabilities/adapters; do not add abstract members to user-implemented interfaces. Absent selector means Active once enabled, regardless of caller software version. Unsupported explicit selection fails rather than silently falling back. Generate wire/proxy output through the repository workflow, never by hand.

| Operation | Enabled contract |
| --- | --- |
| Legacy full Replay | At admission bind the Active execution revision and source-history identity. Start/resume a distinct repair generation at that revision, not a pending desired-definition candidate; retain existing JobId/NotSet availability behavior. |
| Repair completion | Use decision 0016 publication and finish only after the repair is served, preserving successful job completion meaning. Do not wait for optional evolution approval or redefine completion as awaiting approval. Existing job failure/cancellation reporting remains meaningful. |
| Concurrent publication | Journal expected Active/epoch. A repair cannot overwrite a different newly served revision by assuming its admission remains current. Revalidate/reconcile through the owning protocol or report failure/cancellation under existing job semantics; never silently retarget to a pending candidate. |
| Existing pins | Remain on the original generation. Repair does not repoint a pin or promise an old in-place revision snapshot. |
| Explicit partition replay | Separate operation: require proven isolated shadow/adoption receipts on the enabled path or clearly refuse it before live mutation. No historical partial writes into the live generation. |
| Desired-definition rebuild | Separate additive command/admission policy; no silent redirection of legacy Replay to that candidate. |

Reducer generation support requires application-provided exact compatible hosts, empty rebuild baseline, correct retries, isolated state and current capacity. Missing capacity means waiting/unavailable, never execution by another implementation. Capability-refuse unsupported reducer/provider slices while keeping their disabled legacy mode; do not advertise parity before end-to-end tests pass.

### Downgrade and restore

Once initialized, flipping the feature flag off does not remove the floor, undo bindings or make old binaries safe. The store remains governed by its authoritative bindings and decision 0016: a flag-off kernel may refuse new generation work, but it never returns that store to legacy containers, reads, Replay or completion semantics. Refuse kernels below the floor before mutation. A supported downgrade requires an explicit deinitialization/recovery procedure with exclusion/drain, reconciled bindings/jobs/pins/provenance and compatible served semantics; absent such proof, refuse downgrade. Do not implicitly reverse migration by renaming/copying read models or equate a version string with safe storage access.

Restore is a new ownership boundary. Keep writers, cleanup and publication closed while reconciling restored binding/catalog/receipts with actual generation data, definitions, obligations and source-history/compliance state. Unknown or mismatched control/data snapshots are not publication proof. Allocate and persist a new store incarnation before granting ownership or resuming jobs; every operation/grant must carry it so delayed pre-restore commands are rejected even when numeric epochs repeat. Preserve recoverable audit without reusing stale authority.

Reestablish source-history revision/invalidation and erasure obligations before making affected generations readable or rollback-eligible. Restored or slow-built data may not resurrect erased values; repair or make affected generations unreadable until compliant. A restored cursor, snapshot or lease alone cannot prove safety.

## Options considered

- **Bridge/floor plus enforceable exclusion and in-place adoption (chosen):** preserves physical data while providing a real ownership boundary; may require bounded interruption.
- **Membership checks or startup patch alone:** cannot exclude unaware binaries/live sessions or handle later enablement and partial adoption.
- **Copy/rename/view migration:** unnecessary data movement and direct-reader contract changes; rejected.
- **Flag-off downgrade or restored numeric epochs:** permits stale owners to resume; rejected.
- **Mutate Active on enabled Replay or wait for evolution approval:** breaks isolation or existing successful completion meaning; a new repair generation is selected instead.

## Default if unanswered

The compatibility choices are accepted. Keep generations disabled until floor/exclusion, initializer, reader adoption and provider safety are proven. Preserve disabled legacy behavior rather than initialize optimistically; no silent downgrade is allowed after initialization.

## Timeline and scope

Additive installation precedes explicit enablement in the minor release. Default-on generations, mandatory approval and forced reader migration are deferred to a future major. Complete provider/reducer slices may arrive separately, but exclusion, repair contracts, provenance and restore safety cannot be deferred for an advertised slice. Implementation/release sequencing belongs to [epic #5155](https://github.com/Cratis/Chronicle/issues/5155).

## Verification

**Done when:** released-image and provider tests prove old-binary/session exclusion, crash-safe later enablement, in-place legacy adoption, compatible repair/job behavior, downgrade refusal and new-incarnation restore. Acceptance alone establishes none of those runtime proofs.

**Verify by:** test the flag absent/off and enabled; enable long after installation; crash after each adoption/reservation/receipt step; adopt advanced declarations with older served data and unknown snapshots; refuse incomplete legacy jobs until drained/repaired. Keep nonempty data, IDs, indexes, positions and failed obligations unchanged during adoption. Exercise unaware old binaries, reconnects, live sessions and delayed legacy DDL after the claimed barrier. Membership-only success is a negative test, not evidence of exclusion.

Run released .NET and generated TypeScript/Kotlin/Elixir/Python wire/read-selector and legacy Replay job tests, including pending desired revision, concurrent promotion, lost responses, pins and unavailable reducer hosts. Restore mismatched metadata/data backups; inject delayed pre-restore writes/publications/cleanup; prove the new incarnation rejects all of them. Race history revision/redaction/erasure against build, restore and rollback and verify non-resurrection. A failed provider/topology proof blocks that capability, not merely its documentation.

## Consequences

Existing physical names/data survive enablement unchanged, while Active-following readers explicitly adopt binding resolution. Protocol-aware bridges and enforceable access exclusion add deployment obligations and may require a bounded stop/start. Restore and downgrade become explicit recovery operations rather than flag toggles. Unknown legacy provenance remains visible until verified reconstruction replaces inference with evidence.
