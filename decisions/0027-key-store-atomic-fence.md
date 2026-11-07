---
id: 0027-key-store-atomic-fence
title: Make encryption key saves and erasure fence writes atomic and monotonic on every key store
status: proposed
stage: none
class: contract
reversibility: costly
decided: —
decider: woksin (to accept)
applies-to:
  - Source/Kernel/Storage/Compliance/**
  - Source/Kernel/Storage.MongoDB/Encryption/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/Encryption/**
  - Source/Kernel/Storage.Vault/**
  - Source/Kernel/Storage.AzureKeyVault/**
  - Source/Kernel/Storage.InMemory/Compliance/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Make encryption key saves and erasure fence writes atomic and monotonic on every key store

Tracking issues: [Cratis/Chronicle#4979](https://github.com/Cratis/Chronicle/issues/4979). Read-side companion:
[#4978](https://github.com/Cratis/Chronicle/issues/4978). Related: #3745, #3826, #3674, #4977.

## Context

The erasure fence (`EncryptionKeyErasure`: `ErasedThrough`, `ErasedKeyFingerprints`, `NewKeyAllowed`) is what keeps an
erased key erased. Every provider reads the fence and then writes with an unconditional write, so concurrent
operations can undo an erasure or destroy a lawful successor (the four cases in #4979: a delayed save restores an
erased key; a stalled authorization overwrites a newer erasure; a first provisioning races an erasure that found
nothing; a retried erasure deletes a successor). #4978 slice A (`869732f6a9`, reverted by `c4ce4ad846`) validated
fences before release, but a read of a fence that can regress or be undone proves nothing, so it waits for this design.

The maintainer decisions recorded on the issues bind this design:

1. Unreachable key store: fail closed. Every configured store must answer before erasure completes or an erased key is served.
2. Erasure in progress: callers can see an explicit "erasure pending" state; the operation is retried until confirmed.
3. Staged rollout over two releases (N writes the new fields without relying on them; N+1 relies on them). No offline upgrade.
4. Azure Key Vault: Chronicle's own database is the single authority for erasure records; Key Vault holds key material only.
5. Erasure request id is optional (not breaking); retries are guaranteed safe for callers that pass it.

This ADR deliberately supersedes decision 3 in one respect (L0 decision D1 below): release N does not merely write the new
fields, it enforces the protocol for its own writers. Writing the fields without enforcing them would let N writers (unguarded
saves, regressing fence writes, unvalidated releases) produce state that N+1 cannot tell apart from protocol-conformant state,
so N writers could defeat the guarantees N+1 relies on. The two-release shape and "no offline upgrade" of decision 3 stand.

### L0 decisions (recorded)

- **D1. N enforces for its own writers.** Supersedes decision 3 as stated above.
- **D2. `ProtocolFloor` = operator attestation + silo-version check.** Both are required. N+1 fails closed when the
attestation is absent, or when the silo-version check contradicts it (any active silo below N, or without a protocol record).
Mechanism: Rollout.
- **D3. `RevocationGrace` default 5 s, configurable** (kernel compliance option), documented with the compliance configuration. C10.
- **D4. Refuse erasure on stores with no fence guarantee** (Azure Key Vault with InMemory Chronicle storage; Vault on a KV v1
mount). The error names the store and the supported alternative (Azure Key Vault with MongoDB or SQL Chronicle storage; Vault
KV v2). Refusal takes effect only with the N+1 switch-on PR. Release N keeps today's behaviour and logs a startup warning
(non-breaking). If the switch-on is semver-breaking for supported setups it goes to woksin as a major; otherwise it is a minor.

## Decision

One contract, implemented with each store's own atomic primitive. A provider-agnostic mechanism does not exist, so the
**contract** is shared and the **suite** enforces it; the mechanism per store is below.

### The contract

C1. **Monotone fence.** The fence floor and the fingerprint set only grow, whatever the interleaving. A recorded
erasure sets `NewKeyAllowed=false`. Writing an older fence over a newer one is impossible, not merely unlikely.

C2. **Conditional authorization.** `AllowNewKeyFor` succeeds only against the fence state the caller observed
(its `Version`). If the fence changed, it fails with `EncryptionKeyFenceChanged` (the caller re-reads and decides). It
is idempotent for a retry of an authorization that already applied. It never authorizes an erasure it did not observe.

C3. **Guarded publication: stage, validate, publish.** Every key write carries the fence state its writer observed
(`Version`, `Lifecycle`) and lands *unpublished*. Only published revisions are readable: every reader, `GetOrAdd`'s
existing-key branch included, treats an unpublished revision as absent (fail closed; never returns or caches it). The
writer then validates against the fence with the linearizable read of C8 (floor, fingerprint, `NewKeyAllowed`, and that
no erasure request with boundary `>=` the writer's observed `Lifecycle` has been raised, i.e. `ErasedLifecycle < StagedLifecycle`) and publishes with a
conditional update of exactly its own staged row; if the fence forbids it, it deletes exactly its own staged row (ownership, C9) and throws
`EncryptionKeyErased`. A fence document or row is created empty *before* the first save or provisioning for an identifier
(idempotent ensure-exists), so a first save always has something to validate against and guard (case 3 is no longer
"an erasure that found nothing"). An erasure orders itself: raise the fence (recording its boundary, C4), sweep every row inside
the boundary, published or not, then verify (C8). Ordering proof: a save validates after staging; the erasure sweeps after
raising. Either the save's validation sees the raise (it undoes), or the save staged before the raise and the sweep deletes it;
a publish that lands after the sweep updates a deleted row and fails. A row published between raise and sweep is
never released: readers validate the fence at release (#4978, shipped in the same release, see Release gate), and the
fence already shows the raise. Release validation uses the **same predicate as the sweep** (one helper, `Covers`): a key is
refused if `revision <= ErasedThrough`, or its fingerprint is recorded, or its `StagedLifecycle <= ErasedLifecycle` (pre-N rows
have no staged lifecycle and are bounded by revision and fingerprint only). So every read path carries the staged lifecycle:
`EncryptionKeyAtRevision` gains `StagedLifecycle` (nullable), and the cache stores it with the key. The restored
`Refuses` (`869732f6a9`, `EncryptionKeyErasureExtensions.cs:25-27`) checks revision and fingerprint only and must be replaced
by `Covers`. The interleaving this closes: fence at floor 1, `Lifecycle` L; a save stages revision 2 with L and validates
(no request yet); an empty-subject erasure raises `{through 1, lifecycle L}`; the save publishes before the sweep; a release
reads the fence: revision 2 > 1 and the fingerprint is unrecorded, but `L <= ErasedLifecycle` refuses it. The sweep then deletes it. SQL publishes with one statement
(`UPDATE keys SET Published=1 WHERE ... AND EXISTS(fence row with the observed Version)`), which also guards the
fence row; Mongo and Vault rely on the ordering proof, because a multi-document transaction would need a replica set,
serialize every save on one document, and adds nothing the proof lacks now that the guard document always exists.

C4. **Erasure is idempotent per request and bounded by an immutable boundary.** Each erasure carries a request id (optional on
the wire; the kernel generates one only to track a single in-process attempt, which is not retry-safe). The first
application records an immutable request `{requestId, through, lifecycle, fingerprints, confirmed}`, where `lifecycle` is the
fence `Lifecycle` at the raise. The delete sweep removes rows whose staged lifecycle is `<=` that boundary or whose revision is
`<= through`, and adds the fingerprints of what it removed to that request; it never expands into a later
lifecycle, and nothing is "re-raised for anything found". An authorized successor has a higher lifecycle and so is
untouched. Request records are never evicted: a retry of an old id is always recognised and returns its original
outcome. The inline log is capped at 1024 requests per identifier; beyond it erasure refuses with `EncryptionKeyRequestLogFull`
(fail closed), never treating an old id as new. Compaction is a kernel operation, not a manual store edit, and preserves
dedup: it moves only *confirmed* records (never pending) to an append-only, never-evicted per-identifier request archive
(Mongo collection, SQL table, Vault path, Azure via the database authority) in this order: (1) insert the record into the archive
(idempotent by id); (2) remove it from the inline log with a fence write conditional on the observed `Version` (which
increments `Version`). Dedup looks up the inline log, then the archive; an erasure with an id makes its fence write
conditional on the `Version` it saw when it checked both, so a compaction in between fails the condition and the erasure
re-evaluates (C5). Because the archive insert precedes the inline removal, an id is always in one of them.
`ErasedLifecycle` (the max request `lifecycle`, kept as a monotone field so release checks need not scan requests) is unaffected. Rows written before release N have no staged lifecycle and
are bounded by `through` only. Without an id the request is bound to the `Lifecycle` observed at its start; if the lifecycle moved
it is refused as superseded. A caller retrying without an id as a brand-new request cannot be told apart from a legitimate
erasure of the successor; decision 5 scopes the guarantee to callers that pass the id.

C5. **Unknown outcome is an error.** A timeout or lost connection on a fence write surfaces as an error. Nothing
reports success or retries blindly. The only automatic retries are compare-and-set conflicts, where a failed
condition proves the write did not apply, and they re-evaluate from a fresh read, bounded and jittered.

C6. **Stores declare what they guarantee.** `IEncryptionKeyStorage` gains `FenceGuarantees` (flags: `MonotonicFence`,
`FencedSave`), default `None` so existing custom stores compile unchanged. A startup check reports the guarantees of every
configured store and the composition (never silent). A store reporting `None` logs a warning in release N. For the built-in
`None` setups (Azure Key Vault on InMemory storage, Vault KV v1) the N+1 switch-on refuses erasure (D4); for custom stores it is
still open (Options for L0, 1).

C7. **Cost.** Keys are cached; the fence is written on provisioning, save, erasure and authorization and read (C8) on
provisioning, save and release. Release validation is not free: the release boundary (C10) adds one strongest-read per
release scope. Slice 6 must benchmark the append and idempotency paths before and after, and states the measured cost; no zero-cost claim.
Acceptance threshold: the median of the append benchmark regresses by at most 2% against the baseline built from the slice 6
base commit, on the same machine and run. The benchmark is `Benchmarks/Chronicle.Benchmarks/AppendBenchmark.cs` and
`AppendManyBenchmark.cs` with a PII-bearing event case added (one encrypted property, so the release path runs), plus an
in-process `ReleaseKeys` microbenchmark in `Chronicle.Benchmarks.HotPaths` (scope with 1 and 100 identifiers, InMemory store).
Idempotency paths are measured and reported without a threshold. Above 2% slice 6 is not mergeable until it is fixed or woksin accepts the cost.

C8. **Reads are linearizable or the operation fails.** Validation reads (C3), the erasure verifying sweep and release reads
use the strongest read the store offers; where that cannot be provided or times out, the operation throws
`EncryptionKeyFenceUnavailable`. No store silently falls back to a weaker read (a `majority` read is not linearizable and
would invalidate the ordering proof). A store that cannot offer one reports `FenceGuarantees.None`.

C9. **Revision ownership before undo.** A writer may only undo what it staged: each staged write carries a unique writer
nonce and the store's own write identity (Mongo `_id` plus nonce, SQL row nonce, Vault version number, Azure version id). Undo is
conditional on that identity; if the revision no longer carries it, the writer does nothing.

C10. **Release linearization and revocation.** A release (append, idempotency, watch update) is permitted if a linearizable fence
read taken inside its release scope shows the key not covered (`Covers`, C3); that read is the linearization point. The scope memo
keeps the dimensions the restored `ReleaseKeys` already has (`869732f6a9`, `ReleaseKeys.cs:82-86,103`: store, event store,
namespace, identifier), so a fence read is never reused across stores, event stores or namespaces; each entry also records the
fence `Version` and the monotonic and wall-clock instants at which its read *started*. It lives only within one document/batch
scope and is dropped on fault. Nothing is memoized across scopes.
Scope duration is bounded at the actual release, not by waiting: immediately before material leaves the scope (ciphertext handed to
the append, plaintext returned, watch update emitted), with no `await` between the check and the hand-off, `ReleaseKeys` checks the
entry's age (max of monotonic and wall-clock elapsed since the read started). If the age exceeds the `RevocationLease`
(`RevocationGrace / 2`, so 2.5 s by default) it re-reads the fence (C8) and refuses if it now covers the key. Revocation: an
erasure is reported confirmed only after every store's verified sweep (C8) *and* `RevocationGrace` (D3, default 5 s,
configurable) has elapsed since the raise. A memo read that started before the raise is therefore older than the lease by the time
confirmation is reported, so any release after confirmation revalidates and is refused; a paused scope cannot resume on its
memo. Residual, documented: a release whose check passed before confirmation and whose process is suspended in the synchronous
hand-off itself (no I/O, no await). Key-cache entries are evicted on every fence operation.

### Fence record (additive)

New fields next to the existing three: `Version` (uint64, incremented by every fence write by a silo running N or later),
`Lifecycle` (uint32, incremented by each authorization), `ErasedLifecycle` (uint32, monotone max of request lifecycles, C3),
`Requests` (immutable request records, C4, never evicted; compacted only to the archive), and `Seal` (a hash over
`Version`, `ErasedThrough`, fingerprints, `NewKeyAllowed`, `Lifecycle`, `ErasedLifecycle`, written with every N write; see Rollout). The
`confirmed` flag becomes true after the verifying sweep and grace, which gives the "erasure pending" status of decision 2. Key rows
gain `Published` (bool), `StagedLifecycle` and `WriterNonce`; pre-N rows read as published with no staged lifecycle.
Existing fields keep their names, types and meaning.

Interface additions, all with default implementations so custom stores are not broken:
`RecordErasureFor(..., EncryptionKeyErasureRequest)` returning the fence with the request's `through`;
`DeleteThroughFor(..., requestId)` (boundary from the recorded request); `ConfirmErasureFor(..., requestId)`; `AllowNewKeyFor(..., observedVersion)`;
`FenceGuarantees`. The existing signatures remain and delegate (request id absent, lifecycle observed by the store).
`EncryptionKeyErasure` gains `Version`, `Lifecycle`, `ErasedLifecycle`, `Requests`, `Seal` as trailing optional record members;
`EncryptionKeyAtRevision` gains `StagedLifecycle` (nullable) as a trailing optional member.

### Per store

**InMemory** (`Storage/Compliance/InMemoryEncryptionKeyStorage.cs`, which already serializes keys and fence on one `Lock`, and
`Storage.InMemory/Compliance/EncryptionKeyStorage.cs`, which uses two `ConcurrentDictionary` instances and no lock, so only the
latter needs one). One lock per store guards keys and fence, so stage, validate and publish are atomic. Fence writes merge
(`max`, union). Not durable by design: a restart loses the fence, so it is never a valid fence authority for a durable key store (see Azure).

**MongoDB.** Fence write: one `findOneAndUpdate` with upsert per operation. `$max` on `erasedThrough`, `$addToSet`
(`$each`) on fingerprints, `$set newKeyAllowed=false`, `$inc version`, `$push` of the request entry, filtered on
`requests.id != requestId` and, without an id, on `lifecycle == observed`. When the filter fails on an existing
document the upsert raises a duplicate key; that is read as "already applied or superseded" and the document is
re-read. Authorization: `findOneAndUpdate({_id, version: observed, newKeyAllowed: false}, {$set newKeyAllowed=true,
$inc lifecycle, $inc version})`; no match means re-read and apply the idempotent-retry rule or throw
`EncryptionKeyFenceChanged`. The ensure-exists upsert creates an empty fence document before any save. Fence writes use `majority`
write concern (#4977); validation and verifying reads use `readConcern: linearizable` on the primary, which a replica set and a
single node both support. If the server refuses or times out the operation throws `EncryptionKeyFenceUnavailable` (C8); there is no
`majority` fallback. Key inserts stage with `published=false` and the writer nonce; publish is `updateOne({_id, nonce}, {$set published=true})`;
implicit revision recomputes the next revision on a duplicate key (bounded). No multi-document transaction (C3). Old silos replace
the whole fence document (`ReplaceOne`); see rollout.

**SQL** (SQL Server, PostgreSQL and SQLite share one code path, `Storage.Sql/.../Encryption/EncryptionKeyStorage.cs`).
Optimistic concurrency, not provider-specific statements: the fence row gets a portable `Version` integer (not
`rowversion`, which SQLite lacks), plus `Lifecycle` and `Requests` (text/JSON) columns, all nullable or defaulted
so an old silo's `INSERT`/native upsert (`NativeUpserts`) still succeeds. A fence write reads the row, computes the
monotone merge, and runs `UPDATE ... WHERE Identifier=@i AND Version=@v`; zero rows or a unique violation on first insert
re-reads and retries (bounded, jittered). Authorization uses the same condition. Saves insert the key row unpublished (the primary
key `Identifier+Revision` is the ordering point), validate on a fresh context, then publish with the single guarded statement of C3, or delete the row by nonce. Notes: the SQL Server
read-committed-snapshot setting already diagnosed in `Database.cs` must not lower the revalidation read below committed.
A native old-silo upsert only updates the columns it lists and may retain `Version`, so a retained `Version` proves nothing
about old writers; the N+1 gate (rollout) does not rely on detecting it. `GetOrAddWithRevisionFor` returning a never-persisted key (#3745) is not in scope beyond not worsening it.

**Vault (KV v2).** CAS is a native primitive: `WriteSecretAsync(path, data, checkAndSet: n, mountPoint)` exists in
the pinned VaultSharp 1.17.5.1 and is already used by provisioning (`VaultEncryptionKeyStorage.cs` `checkAndSet: 0`);
the version to condition on is `Data.Metadata.Version` of the read. KV v2 CAS exists on every Vault with the v2 engine
(0.10 and later), so the server floor is "KV v2". The spec fixture uses `hashicorp/vault:latest`
(`Storage.Vault.Specs/VaultFixture.cs`), which is unpinned; slice 3 pins a tag. The fence is its own secret
(`ErasurePath`): read with version, merge, write with `checkAndSet = version` (0 when absent); a mismatch re-reads and
re-evaluates under the same rules as Mongo. Key writes use CAS 0 for a new revision (an explicit overwrite uses the
read version), stage with `published=false` and a writer nonce, validate, then publish by CAS on the version it wrote. Undo (C9) reads the path, confirms nonce and version, and destroys that version only with `DestroySecretVersions` (never the path-wide `DeleteMetadataAsync` that `DeleteFor` uses today at `VaultEncryptionKeyStorage.cs:186-214`). A KV v1 mount ignores CAS and would silently
lose the guarantee, so the startup check performs a probe (write a probe secret with CAS 0 twice; the second must be refused
with `check-and-set`) and reports `None` when it is not refused (warning in N, refusal from N+1, D4). Vault's `cas_required` mount option is
recommended for operators but not assumed.
Linearizable reads (C8): Vault serves KV reads from the active node; standbys forward, but Enterprise performance standbys
answer reads locally and may be stale. Every fence and validation read therefore sends `X-Vault-Inconsistent: forward-active-node`
(through VaultSharp's request hook, `VaultClientSettings.BeforeApiRequestAction`, which slice 3 verifies in 1.17.5.1), and the startup
check reads `sys/health` on the configured address: if it reports a performance standby and the forwarding header is rejected,
the store reports `None`. The operator requirement (address the active node or allow forwarding; no client-side eventual-consistency
mode) is documented with the Vault store. A read that cannot be forwarded throws `EncryptionKeyFenceUnavailable`, never a stale answer.

**Azure Key Vault.** There is no conditional write. `SecretClient.SetSecret` (Azure.Security.KeyVault.Secrets 4.11.2)
takes no ETag or `If-Match` condition, `SecretProperties` exposes no ETag, and version listing only lets readers elect a winner
after the fact (which is what `ProvisioningCandidateElection` does for provisioning). Slice 4 re-verifies that against the
package surface before relying on it. Per decision 4 the fence authority is Chronicle's own database, not a secret:
`AzureKeyVaultEncryptionKeyStorage` is given an `IEncryptionKeyFence` implemented by the Mongo and SQL stores (the same
atomic fence writes as above, extracted into that small interface) and keeps only key material.
The save path is write a new secret version, validate against the database fence, then publish by recording the exact version id in the
database authority (`IEncryptionKeyFence.PublishRevision`, conditional on the observed fence); readers read only the published version id. Undo
is scoped to the writer's own version (C9): disable that version (`UpdateSecretProperties` on the exact version); deleting or purging by secret name
is only done by the erasure sweep, never by a save, because name-wide deletion cannot be conditional on the version written. Existing fence secrets (`SecretNames` fence names) are read
and merged into the database fence on first use per identifier (monotone merge, so merging twice is harmless), are
never written once release N+1 relies on the database, and are never deleted automatically. During N the fence in
effect is the union of both; an unreadable one (anything but not found) fails closed. When Chronicle runs on InMemory storage, the
database fence is not durable, so Azure Key Vault reports `None`: release N logs the startup warning and keeps today's behaviour;
from the N+1 switch-on erasure is refused with an error naming the store and the alternative (D4). There is no silent fallback to a weaker design. The sec1 naming (`SecretNames`, `SecretLocations`) is unchanged and key
secrets stay where they are.

**Composite and cache.** The composite sends the same request to every store, raises every fence before any delete
(already the order in `PIIManager.DeleteEncryptionKey`), treats an unreachable or `None` store as "erasure pending" and
throws `EncryptionKeyErasureIncomplete` as today, and merges fences with `max`/union. The cache never caches the fence;
every fence-changing operation evicts, as now.

### Contract suite

An abstract suite in `Storage.Specs` (`Compliance/for_EncryptionKeyStorageContract/`) that every store inherits with its harness,
as `Storage.Sql.Specs` already does with `KeyStoreHarness`. Mongo, SQL, InMemory already reference `Storage.Specs`;
Vault and Azure projects get the reference. The scenarios are gate-controlled (a hook between steps, no sleeps):
a save paused after its check then an erasure then resume (case 1); a stalled authorization over a newer erasure
(case 2); first provisioning racing an erasure of nothing (case 3); a retry with the same request id after a successor
exists, and a retry without an id after authorization (case 4); fence floor and fingerprints never decrease under random interleavings of
record, authorize and save; unknown outcome surfaces; `FenceGuarantees` matches behaviour. Providers that need a container run the same
suite in CI integration; the Vault and Azure suites run non-container against fakes that implement the real write semantics (CAS; no
conditional write plus the DB fence). Vault also runs the full contract suite in CI integration against the pinned real server,
including the publication-versus-sweep interleaving (a save's CAS publish racing the erasure's sweep and version destroy) and
the forwarding header; the fake is not trusted for CAS interaction on its own.

### Rollout and compatibility (two releases)

- **Release N** enforces the whole protocol itself: its writers use the atomic monotone fence writes, stamp `Version`/`Lifecycle`,
record immutable requests, stage and publish, and treat a missing `Version` as 0 (a conditional write on "absent or 0" stamps it). So N does
not depend on N+1 for the guarantees among N writers (D1). Old fields keep their meaning. Silos older than N can still replace a fence
or upsert a key and are outside the guarantee. N also writes a protocol record per silo (`{silo address, cluster id, Chronicle
version, protocol N}`, refreshed on start and with each membership heartbeat) to the fence authority, and logs the D4 startup warning.
- **Release N+1** activates the guarantees against the cluster, gated, fail closed (D2): erasure and release validation are refused
with a diagnostic unless both hold: (a) a durable `ProtocolFloor` attestation record (operator, time, cluster id, minimum version N)
exists in the fence authority, and (b) the silo-version check passes: every *active* member of the Orleans cluster membership
(`IClusterMembershipService`) has a protocol record `>= N`. Missing attestation, or a member that is below N or has no record,
fails closed. The check runs at startup, on every membership change (subscription to membership updates), and before each erasure,
not once. **Stale attestation**: when the check is contradicted the kernel marks the attestation invalidated (durable, with the
offending silo), and it stays invalid after the old silo leaves; the operator must attest again. An attestation for another
cluster id is ignored.
- **Excluding incompatible writers** in the window before detection, and writers outside the cluster: (1) *Fence overwrite*: every
N read verifies `Seal`; an old writer's Mongo `ReplaceOne` drops it and an old SQL upsert changes the sealed columns without
updating it (which is why a retained `Version` was not enough), so a mismatched or missing seal on a fence that has a `Version`
is treated as tampering: the fence is unavailable for that identifier (releases refused, erasure pending) and the attestation is
invalidated. Detection is immediate on the next read; the regressed values are not trusted. (2) *Key injection*: when
attestation is first accepted N+1 backfills `Published=true` on every pre-N key row, records that on the attestation, and from then on
treats a row without `Published` as unpublished (absent). Rows an old writer inserts later are invisible. (3) *Old deletes*
(the pre-N erasure path) can only remove keys: a loss of availability, never an erased key served. (4) *Direct storage users*
(processes writing the key store or fence other than through Chronicle) are unsupported; the attestation states there are none,
and the seal and the `Published` rule make their writes detected or invisible rather than trusted. Detection of old writers by a
missing `Version` alone is not used. N+1 requires every silo to run N or later; no offline upgrade.
- **Unsupported stores (D4)**: the refusal of erasure on `None` stores ships in the N+1 switch-on PR, which is classified there:
major via woksin if it breaks a supported setup, otherwise minor.
- **Old silos reading new documents**: Mongo old class mapping must ignore unknown elements (spec). SQL: new columns nullable or
defaulted; pre-N rows read as published. Vault: extra fields ignored by the old reader. Azure: old silos keep reading the old fence secret.
- No change to key material layout, secret names, or revisions. Roll-back from N+1 to N is safe; from N to the previous release loses
protection only for fences written while old silos ran.

### Restoring #4978 slice A on top

Slice A was parked (`c4ce4ad846`) on unresolved ordering, completion and cache-revocation proofs, not on a failing test; reverting the revert
is not enough. This ADR supplies them: ordering = C3; completion = C4 (confirmed only after verified sweep and grace) plus C8; cache revocation = C10.
Release N must not ship without slice 6 (release-time validation), because publication without release validation leaves the window between raise and sweep open.
Order: slices 0 to 5 land, then `git revert c4ce4ad846` (re-applies `869732f6a9`, 80 files), then adapt:

- `GetErasureForRelease` becomes the C8 read, returns `Version`, throws `EncryptionKeyFenceUnavailable` on any non-answer.
- The restored `ReleaseKeys` memo (`869732f6a9`, `ReleaseKeys.cs:82-86,103`) is already keyed by store, event store, namespace and
identifier; keep those dimensions, add the `Version` and read-start instants to each entry, drop entries on fault, and add the
lease check at the hand-off (C10).
- `Refuses` is replaced by `Covers` (C3), and every read path, the cache included, carries `StagedLifecycle`.
- The cache publishes only after C3 publication; the composite requires every configured fence and fails closed on `None`.
- Slice A specs run against the contract fakes, and include reads inside the stage-to-publish window (including `GetOrAdd`'s existing-key branch).

## Options for L0

Decided and moved to "L0 decisions" (D1-D4): N enforcement versus decision 3, `ProtocolFloor` source, `RevocationGrace`, and
refusal for Azure Key Vault on InMemory storage and Vault KV v1. Still open, with recommendations:

1. Custom or third-party `IEncryptionKeyStorage` without `FenceGuarantees` (D4 covers only the built-in `None` setups): warn in N,
then refuse erasure in the N+1 switch-on behind `RequireAtomicKeyFence`, or keep warning? Recommended: refuse behind the flag
(default true, opt-out documented, removed next major), classified with the switch-on under D4's semver rule.
2. Request id on the wire: opaque string, up to 128 characters (recommended).

## Review resolution (review-r1)

| Finding | Verified | Change |
|---|---|---|
| B1 unvalidated exposure; empty-subject delayed save | Yes (`EncryptionKeyErasureExtensions.cs:38` checks floor/fingerprint only) | C3 stage/validate/publish, unpublished is unreadable, saves bound to lifecycle, fence created first, release validation ships in N (slice 6 moved into N); specs read inside the window |
| B2 re-raise captures successor; eviction | Yes | C4 immutable boundary (lifecycle and through), no re-raise, no eviction, cap fails closed |
| B3 majority not linearizable; absent fence | Yes | C8 linearizable or `EncryptionKeyFenceUnavailable`; ensure-exists guard doc; transactions reconsidered and rejected for Mongo, SQL uses one guarded statement |
| B4 N does not enforce | Yes | N enforces protocol; N+1 gated on `ProtocolFloor`; missing-Version detection dropped |
| B5 memo and revocation | Yes | C10 boundary, per-identifier memo, grace; C7 benchmark; restored memo task replaced |
| Advisory: stale baseline | Yes (`Storage/Compliance/InMemoryEncryptionKeyStorage.cs:18-27` already locks; `Storage.InMemory/Compliance/EncryptionKeyStorage.cs` does not; SQL persist-or-read-winner exists at `EncryptionKeyStorage.cs:98-109`) | InMemory scope narrowed; SQL notes keep the winner-read; baseline corrected |
| Advisory: Vault/Azure undo name-wide | Yes (`DeleteFor` uses `DeleteMetadataAsync` path-wide) | C9 ownership; Vault destroys own version, Azure disables own version and publishes by version id |
| Restoration proofs | Yes | Restoring section supplies ordering, completion, revocation proofs |

## Review resolution (review-r2)

| Finding | Verified | Change |
|---|---|---|
| B1 release validation ignores lifecycle | Yes (`869732f6a9`: `EncryptionKeyErasureExtensions.cs:25-27` `Refuses` checks revision and fingerprint only; `EncryptionKeyAtRevision.cs:13` has no lifecycle) | C3: release uses the sweep predicate `Covers` (incl. `StagedLifecycle <= ErasedLifecycle`); `EncryptionKeyAtRevision.StagedLifecycle` carried through reads and cache; monotone `ErasedLifecycle`; the exact interleaving is a slice 6 spec |
| B5 scope duration unbounded; memo dimensions | Yes (the grace only delayed confirmation; `ReleaseKeys.cs:82-86,103` memo is per store, event store, namespace, identifier, so r1's "one fence task" was wrong) | C10: lease check at the hand-off with re-read beyond `RevocationGrace / 2`; memo keeps all four dimensions plus `Version`; residual narrowed to a suspended synchronous hand-off |
| B4 attestation without admission | Yes | Rollout: attestation + membership silo-version check, re-evaluated on membership change and before erasure; durable invalidation needing re-attestation; `Seal` detects old fence overwrites; `Published` backfill makes old-writer keys invisible; direct storage users unsupported, detected or invisible |
| Advisory B2 compaction | Yes | C4: kernel compaction of confirmed records to an append-only archive, archive-then-remove, `Version`-conditioned dedup |
| Advisory B3 Vault linearizable reads | Yes | Vault: `X-Vault-Inconsistent: forward-active-node`, `sys/health` performance-standby check, `None` otherwise, documented operator requirement |
| Advisory Vault CAS on a real server | Yes | Contract suite: full Vault suite in CI integration incl. publish-versus-sweep |
| Advisory benchmark threshold | Yes | C7: at most 2% median append regression; benchmarks named |
| L0 decisions | n/a | D1-D4 recorded; decision 3 superseded with reason; decided items removed from Options for L0 |
