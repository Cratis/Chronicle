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
no erasure request with boundary `>=` the writer's observed `Lifecycle` has been raised) and publishes with a
conditional update of exactly its own staged row; if the fence forbids it, it deletes exactly its own staged row (ownership, C9) and throws
`EncryptionKeyErased`. A fence document or row is created empty *before* the first save or provisioning for an identifier
(idempotent ensure-exists), so a first save always has something to validate against and guard (case 3 is no longer
"an erasure that found nothing"). An erasure orders itself: raise the fence (recording its boundary, C4), sweep every row inside
the boundary, published or not, then verify (C8). Ordering proof: a save validates after staging; the erasure sweeps after
raising. Either the save's validation sees the raise (it undoes), or the save staged before the raise and the sweep deletes it;
a publish that lands after the sweep updates a deleted row and fails. A row published between raise and sweep is
never released: readers validate the fence at release (#4978, shipped in the same release, see Release gate), and the
fence already shows the raise. A delayed save at revision 2 under an empty-subject fence at floor 1 is caught because the
boundary is by lifecycle as well as revision (C4), not by revision alone. SQL publishes with one statement
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
outcome. The log is capped at 1024 requests per identifier; beyond it erasure refuses with `EncryptionKeyRequestLogFull`
(fail closed, operator compaction documented), never treating an old id as new. Rows written before release N have no staged lifecycle and
are bounded by `through` only. Without an id the request is bound to the `Lifecycle` observed at its start; if the lifecycle moved
it is refused as superseded. A caller retrying without an id as a brand-new request cannot be told apart from a legitimate
erasure of the successor; decision 5 scopes the guarantee to callers that pass the id.

C5. **Unknown outcome is an error.** A timeout or lost connection on a fence write surfaces as an error. Nothing
reports success or retries blindly. The only automatic retries are compare-and-set conflicts, where a failed
condition proves the write did not apply, and they re-evaluate from a fresh read, bounded and jittered.

C6. **Stores declare what they guarantee.** `IEncryptionKeyStorage` gains `FenceGuarantees` (flags: `MonotonicFence`,
`FencedSave`), default `None` so existing custom stores compile unchanged. A startup check reports the guarantees of every
configured store and the composition (never silent). A store reporting `None` logs a warning in release N. Whether
release N+1 refuses erasure on such a store is a compat decision in the L0 options below.

C7. **Cost.** Keys are cached; the fence is written on provisioning, save, erasure and authorization and read (C8) on
provisioning, save and release. Release validation is not free: the release boundary (C10) adds one strongest-read per
release scope. Slice 6 must benchmark the append and idempotency paths before and after, and states the measured cost; no zero-cost claim.

C8. **Reads are linearizable or the operation fails.** Validation reads (C3), the erasure verifying sweep and release reads
use the strongest read the store offers; where that cannot be provided or times out, the operation throws
`EncryptionKeyFenceUnavailable`. No store silently falls back to a weaker read (a `majority` read is not linearizable and
would invalidate the ordering proof). A store that cannot offer one reports `FenceGuarantees.None`.

C9. **Revision ownership before undo.** A writer may only undo what it staged: each staged write carries a unique writer
nonce and the store's own write identity (Mongo `_id` plus nonce, SQL row nonce, Vault version number, Azure version id). Undo is
conditional on that identity; if the revision no longer carries it, the writer does nothing.

C10. **Release linearization and revocation.** A release (append, idempotency, watch update) is permitted if a linearizable fence
read taken inside its release scope shows the key not erased; that read is the linearization point. The scope memo is keyed by
`(identifier, Version)`, one entry per identifier (not one task for the whole fence), lives only within one document/batch scope and is dropped on fault.
Nothing is memoized across scopes. Revocation: an erasure is reported confirmed only after every store's verified sweep (C8) *and* a
`RevocationGrace` (default 5 s, configurable) has elapsed since the raise, which bounds the lifetime of any in-flight scope
memo; material released by a scope that read the fence before the raise is allowed to complete within that grace and is the only
documented residual. Key-cache entries are evicted on every fence operation.

### Fence record (additive)

New fields next to the existing three: `Version` (uint64, incremented by every fence write by a silo running N or later),
`Lifecycle` (uint32, incremented by each authorization), `Requests` (immutable request records, C4, never evicted). The
`confirmed` flag becomes true after the verifying sweep and grace, which gives the "erasure pending" status of decision 2. Key rows
gain `Published` (bool), `StagedLifecycle` and `WriterNonce`; pre-N rows read as published with no staged lifecycle.
Existing fields keep their names, types and meaning.

Interface additions, all with default implementations so custom stores are not broken:
`RecordErasureFor(..., EncryptionKeyErasureRequest)` returning the fence with the request's `through`;
`DeleteThroughFor(..., requestId)` (boundary from the recorded request); `ConfirmErasureFor(..., requestId)`; `AllowNewKeyFor(..., observedVersion)`;
`FenceGuarantees`. The existing signatures remain and delegate (request id absent, lifecycle observed by the store).
`EncryptionKeyErasure` gains `Version`, `Lifecycle`, `Requests` as trailing optional record members.

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
with `check-and-set`) and reports `None` and refuses erasure when it is not refused. Vault's `cas_required` mount option is
recommended for operators but not assumed.

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
database fence is not durable, so Azure Key Vault reports `None` and refuses erasure with the startup diagnostic; there is
no silent fallback to a weaker design. The sec1 naming (`SecretNames`, `SecretLocations`) is unchanged and key
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
conditional write plus the DB fence), and a one-off container spec checks the fake against the real server.

### Rollout and compatibility (two releases)

- **Release N** enforces the whole protocol itself: its writers use the atomic monotone fence writes, stamp `Version`/`Lifecycle`,
record immutable requests, stage and publish, and treat a missing `Version` as 0 (a conditional write on "absent or 0" stamps it). So N does
not depend on N+1 for the guarantees among N writers. Old fields keep their meaning. Silos older than N can still replace a fence
or upsert a key and are outside the guarantee.
- **Release N+1** activates the guarantees against the cluster, gated, fail closed: erasure and release validation are refused
with a diagnostic unless a durable `ProtocolFloor` record in the fence authority says every silo runs N or later. The floor is
set from grain-compatibility/silo-version information if the tooling exposes it, otherwise by explicit operator attestation
(L0 option 4). Detection of old writers by a missing `Version` is not used, because old SQL upserts retain `Version`. N+1
requires every silo to run N or later; no offline upgrade.
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
- The restored `ReleaseKeys` memoizes one fence task (`869732f6a9`, `ReleaseKeys.cs:74-86`); replace by the per-identifier `(identifier, Version)` memo of C10, scoped to one release scope and dropped on fault.
- The cache publishes only after C3 publication; the composite requires every configured fence and fails closed on `None`.
- Slice A specs run against the contract fakes, and include reads inside the stage-to-publish window (including `GetOrAdd`'s existing-key branch).

## Options for L0

Genuine product/compat decisions, with recommendations:

1. Custom `IEncryptionKeyStorage` without `FenceGuarantees`: warn in N, refuse erasure in N+1 behind `RequireAtomicKeyFence` (recommended; default true in N+1, removed next major).
2. Azure Key Vault with InMemory Chronicle storage, and Vault on a KV v1 mount: refuse erasure with a diagnostic (recommended).
3. Maintainer decision 3 said N "does not rely on" the new fields. This ADR has N enforce the protocol for its own writers and gates N+1 on `ProtocolFloor`. Confirm.
4. `ProtocolFloor` source: silo-version tooling if it exposes it, else operator attestation (recommended: attestation fallback, never inferred).
5. `RevocationGrace` default 5 s: erasure confirmation latency against the residual release window. Request id on the wire: opaque string, up to 128 chars (recommended).

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

## Options for L0

A genuine compat/product decision, with recommendations:

1. Custom or third-party `IEncryptionKeyStorage` without `FenceGuarantees`: warn in N, refuse erasure in N+1, or keep warning? Recommend refuse in N+1 behind `RequireAtomicKeyFence` (default true in N+1, opt-out documented, removed next major).
2. Azure Key Vault with InMemory Chronicle storage: refuse erasure (recommended), or allow with a warning? Refuse; InMemory is not a durable authority.
3. Vault on a KV v1 mount: unsupported already in practice; recommend refuse erasure with a diagnostic, no new store drop.
