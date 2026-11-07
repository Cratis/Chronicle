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

C3. **Fenced saves: write, then validate, then undo.** `SaveFor` and provisioning keep their pre-check (fast fail),
write the key, then re-read the fence with the strongest read the store offers and compare against the written
revision and fingerprint. If the fence forbids it, the writer removes exactly the revision it wrote and throws
`EncryptionKeyErased`. An erasure always orders itself as: raise the fence, delete revisions up to the floor the
request recorded, then sweep again and re-raise for anything found (bounded loop). Because the raise precedes the
delete sweep and every save validates after its write, one of them always wins: a save that wrote before the raise is
swept; a save that wrote after the raise validates against it. This covers case 1 and case 3 (a first insert
at revision 1 racing an erasure that found nothing) on every store without needing a cross-document transaction.
Between the write and the undo the key exists; readers must not release unvalidated material (#4978, slice 6).

C4. **Erasure is idempotent per request.** Each erasure carries a request id (optional on the wire; the kernel
generates one only to track a single in-process attempt, which is not retry-safe). The fence records
`{requestId, through}` for recent requests. A retry with a known id re-applies nothing and returns the original
`through`; the delete sweep is bounded to `through` and to the fingerprints of that request, so it can never touch a
revision above the floor of its own request, which is where a successor lives. Without an id the request is bound to
the `Lifecycle` observed at its start; if the lifecycle moved (a new key was authorized), it is refused as
superseded instead of erasing the successor. A caller that retries without an id as a brand-new request cannot be told
apart from a legitimate new erasure of the successor; that limit is documented, and decision 5 already scopes the
guarantee to callers that pass the id.

C5. **Unknown outcome is an error.** A timeout or lost connection on a fence write surfaces as an error. Nothing
reports success or retries blindly. The only automatic retries are compare-and-set conflicts, where a failed
condition proves the write did not apply, and they re-evaluate from a fresh read, bounded and jittered.

C6. **Stores declare what they guarantee.** `IEncryptionKeyStorage` gains `FenceGuarantees` (flags: `MonotonicFence`,
`FencedSave`), default `None` so existing custom stores compile unchanged. A startup check reports the guarantees of every
configured store and the composition (never silent). A store reporting `None` logs a warning in release N. Whether
release N+1 refuses erasure on such a store is a compat decision in the L0 options below.

C7. **No cost on the append path.** Keys are cached; the fence is read and written only on provisioning, save,
erasure and authorization. Release-time validation (#4978) adds one memoized fence read per release scope, owned by
that issue, not by this one.

### Fence record (additive)

New fields next to the existing three: `Version` (uint64, incremented by every fence write by a new silo),
`Lifecycle` (uint32, incremented by each authorization), `Requests` (the most recent 64 `{id, through, confirmed}`
entries; an entry older than that falls out, and the limit is documented). `confirmed` becomes true after the verifying
sweep, which gives the "erasure pending" status of decision 2. Existing fields keep their names, types and meaning.

Interface additions, all with default implementations so custom stores are not broken:
`RecordErasureFor(..., EncryptionKeyErasureRequest)` returning the fence with the request's `through`;
`DeleteThroughFor(..., through)`; `ConfirmErasureFor(..., requestId)`; `AllowNewKeyFor(..., observedVersion)`;
`FenceGuarantees`. The existing signatures remain and delegate (request id absent, lifecycle observed by the store).
`EncryptionKeyErasure` gains `Version`, `Lifecycle`, `Requests` as trailing optional record members.

### Per store

**InMemory** (both `Storage.InMemory/Compliance/EncryptionKeyStorage.cs` and `Storage/Compliance/InMemoryEncryptionKeyStorage.cs`).
One lock per store guards keys and fence together, so save-with-check and every fence write are atomic. Fence
writes merge (`max`, union). The second class is a plain `Dictionary` today and gets the lock. Not durable by design: it
reports guarantees but a restart loses the fence, so it is never a valid fence authority for a durable key store (see Azure).

**MongoDB.** Fence write: one `findOneAndUpdate` with upsert per operation. `$max` on `erasedThrough`, `$addToSet`
(`$each`) on fingerprints, `$set newKeyAllowed=false`, `$inc version`, `$push` of the request entry, filtered on
`requests.id != requestId` and, without an id, on `lifecycle == observed`. When the filter fails on an existing
document the upsert raises a duplicate key; that is read as "already applied or superseded" and the document is
re-read. Authorization: `findOneAndUpdate({_id, version: observed, newKeyAllowed: false}, {$set newKeyAllowed=true,
$inc lifecycle, $inc version})`; no match means re-read and apply the idempotent-retry rule or throw
`EncryptionKeyFenceChanged`. Fence writes use `majority` write concern (#4977). The revalidation read in C3 and the
erasure verifying sweep use a linearizable read on a replica set (primary read, `majority` otherwise) and `local` on a
single node, which is linearizable by itself; the store detects which at first use. Key inserts: explicit revision
becomes insert-then-undo; implicit revision recomputes the next revision on a duplicate key (bounded). No multi-document
transaction: a transaction cannot make a save conflict with a fence document that does not exist yet (case 3), and the
write/validate/undo order already closes the cases. Old silos replace the whole fence document (`ReplaceOne`); see
rollout.

**SQL** (SQL Server, PostgreSQL and SQLite share one code path, `Storage.Sql/.../Encryption/EncryptionKeyStorage.cs`).
Optimistic concurrency, not provider-specific statements: the fence row gets a portable `Version` integer (not
`rowversion`, which SQLite lacks), plus `Lifecycle` and `Requests` (text/JSON) columns, all nullable or defaulted
so an old silo's `INSERT`/native upsert (`NativeUpserts`) still succeeds. A fence write reads the row, computes the
monotone merge, and runs `UPDATE ... WHERE Identifier=@i AND Version=@v`; zero rows or a unique violation on first insert
re-reads and retries (bounded, jittered). Authorization uses the same condition. Saves insert the key row (the primary
key `Identifier+Revision` is the ordering point), then revalidate on a fresh context, then undo. Notes: the SQL Server
read-committed-snapshot setting already diagnosed in `Database.cs` must not lower the revalidation read below committed.
A native old-silo upsert only updates the columns it lists, so it does not bump `Version`; that is part of the
mixed-version window below. `GetOrAddWithRevisionFor` returning a never-persisted key (#3745) is not in scope beyond not worsening it.

**Vault (KV v2).** CAS is a native primitive: `WriteSecretAsync(path, data, checkAndSet: n, mountPoint)` exists in
the pinned VaultSharp 1.17.5.1 and is already used by provisioning (`VaultEncryptionKeyStorage.cs` `checkAndSet: 0`);
the version to condition on is `Data.Metadata.Version` of the read. KV v2 CAS exists on every Vault with the v2 engine
(0.10 and later), so the server floor is "KV v2". The spec fixture uses `hashicorp/vault:latest`
(`Storage.Vault.Specs/VaultFixture.cs`), which is unpinned; slice 3 pins a tag. The fence is its own secret
(`ErasurePath`): read with version, merge, write with `checkAndSet = version` (0 when absent); a mismatch re-reads and
re-evaluates under the same rules as Mongo. Key writes use CAS 0 for a new revision (an explicit overwrite uses the
read version), then validate and undo by deleting that path's metadata. A KV v1 mount ignores CAS and would silently
lose the guarantee, so the startup check performs a probe (write a probe secret with CAS 0 twice; the second must be refused
with `check-and-set`) and reports `None` and refuses erasure when it is not refused. Vault's `cas_required` mount option is
recommended for operators but not assumed.

**Azure Key Vault.** There is no conditional write. `SecretClient.SetSecret` (Azure.Security.KeyVault.Secrets 4.11.2)
takes no ETag or `If-Match` condition, `SecretProperties` exposes no ETag, and version listing only lets readers elect a winner
after the fact (which is what `ProvisioningCandidateElection` does for provisioning). Slice 4 re-verifies that against the
package surface before relying on it. Per decision 4 the fence authority is Chronicle's own database, not a secret:
`AzureKeyVaultEncryptionKeyStorage` is given an `IEncryptionKeyFence` implemented by the Mongo and SQL stores (the same
atomic fence writes as above, extracted into that small interface) and keeps only key material.
The save path is write, validate against the database fence, then delete and purge the secret revision if it is fenced
(the existing purge path and `EncryptionKeyNotPurged` still apply). Existing fence secrets (`SecretNames` fence names) are read
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

- **Release N** writes the new fields with the new atomic operations and keeps the old fields exactly as they were. The
monotone updates are a superset of the old behaviour, so floor and fingerprints are protected against regression from
every new silo already in N. N does not rely on `Version`, `Lifecycle` or `Requests` for decisions.
- **Release N+1** relies on them (C2, C4). A fence without `Version` was last written by an old silo (it replaced the
document and dropped the unknown fields); N+1 treats it as legacy: atomically stamps `Version=1,Lifecycle=0`, never relies on
`Requests` for it, and logs a diagnostic. N+1 requires every silo to run N or later; the upgrade guide says so and the startup
check logs the count of legacy-stamped fences.
- **Old silos reading new documents**: Mongo maps with the existing conventions; the implementer must prove by spec that
old class mapping ignores unknown elements (`IgnoreExtraElements`). SQL: new columns are nullable or defaulted. Vault: extra
secret fields are ignored by the old reader (it reads three named fields). Azure: old silos keep reading the old fence secret.
- **Old silos writing**: an old silo can still replace a fence document or row; regression protection against old
silos starts when every silo runs N. This is stated in the release notes and the issue closes on N+1.
- No change to key material layout, secret names, or revisions. No offline upgrade. Roll-back from N+1 to N is safe;
from N to the previous release loses protection only for fences written while old silos ran.

### Restoring #4978 slice A on top

Order: slices 0 to 5 land first, then `git revert c4ce4ad846` (re-applies `869732f6a9`), then adapt. Slice A is 80 files
(`ReleaseKeys`, `PIICompliancePropertyValueHandler`, `AppendFingerprintComparison`, cache and composite fail-closed, specs). What it
needs from this design, so it is not reverted again:

- `GetErasureForRelease` (added to the interface by slice A) becomes the strongest-read fence read of C3, returns
`Version`, and throws `EncryptionKeyFenceUnavailable` on any non-answer.
- Memo scopes (document, batch, watch update) key on fence `Version`; a version change inside a scope drops the memo.
- The cache never publishes a saved key before the validate-and-undo of C3 finished.
- The composite requires every configured store's fence for release and provisioning (decision 1); `None` stores fail closed.
- The slice A specs that assumed a store whose fence cannot regress run against the contract-tested fakes, not ad-hoc stubs.

## Options for L0

A genuine compat/product decision, with recommendations:

1. Custom or third-party `IEncryptionKeyStorage` without `FenceGuarantees`: warn in N, refuse erasure in N+1, or keep warning? Recommend refuse in N+1 behind `RequireAtomicKeyFence` (default true in N+1, opt-out documented, removed next major).
2. Azure Key Vault with InMemory Chronicle storage: refuse erasure (recommended), or allow with a warning? Refuse; InMemory is not a durable authority.
3. Vault on a KV v1 mount: unsupported already in practice; recommend refuse erasure with a diagnostic, no new store drop.

## Open questions

1. Request id type on the wire: a string (recommended, opaque, ≤128 chars) over a `Guid`; optional field added to the erasure request in `Contracts` (generated protocol, additive).
2. `Requests` retention of 64 per identifier: recommend 64; an id older than that is treated as new (documented).
3. Mongo linearizable reads on replica sets: recommend yes for the two rare reads; fall back to `majority` with a logged note if the deployment forbids it.
4. Should "erasure pending" be exposed by a new query or only by the existing completion error? Recommend the fence flag now, query surface in a follow-up issue.
5. Is the second release gate enforced mechanically (a cluster-version check) or by documentation only? Recommend documentation plus the legacy-stamp diagnostic; a cluster-version check if the grain compatibility tooling already exposes silo versions (to be checked in slice 5).
