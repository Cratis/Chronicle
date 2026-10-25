---
id: 0019-unique-constraint-encoding-v2-and-offline-migration
title: Encode unique-constraint values and scopes as keyed v2 tokens, and move stores to them through an offline, fenced, backup-protected cutover in a major release
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-25
decider: L0 (woksin's delegation)
applies-to:
  - Source/Kernel/Core/Events/Constraints/**
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Compliance/GDPR/**
  - Source/Kernel/Core/Patches/**
  - Source/Kernel/Concepts/Events/Constraints/**
  - Source/Kernel/Storage/Events/Constraints/**
  - Source/Kernel/Storage/Compliance/**
  - Source/Kernel/Storage.InMemory/Events/Constraints/**
  - Source/Kernel/Storage.MongoDB/Events/Constraints/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/UniqueConstraints/**
  - Source/Infrastructure/Schemas/**
---

# Encode unique-constraint values and scopes as keyed v2 tokens, and move stores to them through an offline, fenced, backup-protected cutover in a major release

Tracking issue: [#5362](https://github.com/Cratis/Chronicle/issues/5362). Part of
[#4384](https://github.com/Cratis/Chronicle/issues/4384),
[#4385](https://github.com/Cratis/Chronicle/issues/4385) and
[#4131](https://github.com/Cratis/Chronicle/issues/4131). This is step 2 of
[#4389](https://github.com/Cratis/Chronicle/issues/4389). It builds on
[decision 0008](0008-repair-constraint-indexes-from-the-log.md), whose rebuild it reuses, and on
[decision 0013](0013-unique-index-entries-carry-their-pii-subject.md), whose erasure rules it keeps. The delete-only
patch for legacy rows ([#4866](https://github.com/Cratis/Chronicle/issues/4866)) is a separately approved,
destructive exception. This record does not cover it.

## Context

Unique-constraint values and scopes are encoded in a way that loses information or aliases keys:

- **[#4384](https://github.com/Cratis/Chronicle/issues/4384).** The v1 value drops null components and loses
  their position, so `("x", null)`, `(null, "x")` and `("x")` all look the same.
- **[#4131](https://github.com/Cratis/Chronicle/issues/4131).** Components are joined with `-`, which can appear
  inside a value.
- **[#4385](https://github.com/Cratis/Chronicle/issues/4385).** The scope key joins its parts with `|` and escapes
  nothing, so different scopes can produce the same key. The scope is also part of table and collection names, which
  PostgreSQL clips at 63 bytes.
- **[#4389](https://github.com/Cratis/Chronicle/issues/4389) step 2.** The value is an unsalted SHA-256. Step 2
  replaces it with a keyed HMAC identified by a `HashVersion`. Nothing in `Source` carries a `HashVersion` today.

Changing the encoding changes every stored value and every v1 table name, so no store can match v1 and v2 claims at
the same time. Previous binaries contain no fencing code, so code in the new release cannot exclude them. Two
mechanisms that already exist do not help: the generation floor is checked once at startup, and the patching record
shows the last kernel that patched, not what is deployed.

If nobody chooses, the three issues stay open, the keyed hash stays unplanned, and any later attempt to change the
encoding in place can silently drop or merge claims. Three rounds of data-loss review of the design
(the last one found three blockers, which this record turns into rules) are the basis for the choices below.

## Decision

### 1. The encoding

One `UniqueConstraintEncoder` with `HashVersion = 2` is the only encoder. Append, replay, reindex, catch-up,
erasure and every storage provider call it.

1. **The value encoded is the value the schema can represent.** The encoder takes the schema-typed CLR value of each
   declared property: on append from the schema-converted plaintext content, on replay and reindex from the stored
   content converted through the same schema after strict release. A JSON literal the schema cannot represent
   (`1.00000001` under `float`) encodes as the value that is stored.
2. **Canonical text per format (UTF-8).**

   | Format | Canonical text |
   | --- | --- |
   | string, no format | the string as is; no Unicode normalization |
   | `guid`, `uuid` | lowercase `D` |
   | `int16`, `int32`, `int64`, `byte`, `uint32`, `uint64` | base 10, invariant, no leading zeros |
   | `float` | `R` of the `Single` value, invariant; `-0` becomes `0` |
   | `double`, number with no format | `R`, invariant; `-0` becomes `0`; NaN and infinity are rejected on append |
   | `decimal` | invariant, trailing fraction zeros removed, no exponent |
   | `date-time`, `date-time-offset` | the UTC instant as `yyyy-MM-ddTHH:mm:ss.fffffffZ`; unspecified kind is UTC |
   | `date`, `time`, `duration` | `yyyy-MM-dd`, `HH:mm:ss.fffffff`, `c` |
   | boolean | `true`, `false` |
   | array of a scalar format | a framed list (see 3) |
   | not in the schema | a string that `Guid.TryParse` accepts becomes lowercase `D`; other strings as is; numbers as `double`; booleans as above |

   `byte-array`, `point`, `linestring`, `polygon`, `object`, `dynamic` and nested arrays are rejected when a
   constraint is registered. An existing constraint that uses one is marked `Incompatible` in the migration.
3. **Framing and hash.**

   ```text
   component := 0x00 | 0x01 u32be(len) text | 0x02 u32be(count) (0x00 | 0x01 u32be(len) text)*
   preimage  := "chronicle/unique/v2" 0x00 u32be(N) component{N}
   value     := lowercase hex(HMAC-SHA256(storeKey, preimage))        -- 64 characters
   ```

   Tag `0x00` is null (JSON null or a missing property), `0x01` a present scalar, `0x02` a present array. An empty
   string is present with length 0, so it differs from null. The framing is injective over the canonical bytes; only
   the HMAC and the scope token keep probabilistic collision bounds.
4. **Nulls.** A null component claims, by position. A tuple where every component is null claims nothing. No kind
   byte is written: the string `"1"` and the integer `1` encode equally, as in v1.
5. **`IgnoreCasing`** applies `ToLowerInvariant()` to string leaves only, including array elements and unknown
   non-GUID strings. Changing the casing behavior needs a new `HashVersion`.
6. **Scope token.**

   ```text
   scopePreimage := "chronicle/scope/v2" 0x00 (0x00 | 0x01 u32be(len) utf8(d)) for d in (EventSourceType, EventStreamType, EventStreamId)
   scopeToken    := first 32 lowercase hex of HMAC-SHA256(storeKey, scopePreimage)
   ```

   The input is the typed `ResolvedConstraintScope`. An unscoped constraint uses the token of the all-unnarrowed
   tuple. The scope is stored as a column, not as part of a name.
7. **Key equivalence is rejected at registration.** A constraint whose claim and removal definitions have unequal
   arity, or that mix kinds of one position (for example a string and an integer), is refused when it is registered.
   The key therefore has one arity N, with no trailing-null padding. This replaces the padding rule of the design
   proposal.
8. **Provider precision (UNVERIFIED).** If a provider stores a temporal or numeric format at lower precision than
   its CLR type (for example MongoDB `BsonDateTime` in milliseconds), the canonical form truncates to the lowest
   precision every provider round-trips. This is fixed from append, persist and replay vectors for every format on
   every provider. A format that cannot be made stable is rejected at registration.

### 2. Erasure and the key

1. **Erased is an explicit status.** Strict release returns, per protected property, `Plain`, `Decrypted` or `Erased`.
   `Erased` means the value is ciphertext and an erasure record exists. A claim with any erased component claims
   nothing. A removal with any erased component releases nothing, because `RemoveForSubject` already deleted that
   subject's claims (decision 0013). A missing key without an erasure record raises `MissingEncryptionKey`, and the
   rebuild fails closed instead of dropping the claim. The `IsNullOrEmpty` skip and the lenient release are
   replaced by this status.
2. **One 256-bit HMAC key per event store**, kept in the event store database and never in subject key storage. It
   is created by an atomic create-if-absent with one durable winner, its `KeyId` is committed to the marker before
   anything else is published, and it is never regenerated.
3. **Store classes**, checked durably in this order:
   - **New store:** no events, no v1 or v2 constraint tables, no marker. It provisions its key on creation, and its
     marker starts at `Verified`.
   - **v1 store:** has events or v1 tables and no marker. A key is never created on first use; it is created only in
     step M1 of an attested cutover. Until then governed appends are refused.
   - **Initialized store:** has a marker. It must resolve exactly the marker's `KeyId`, or every unique-constraint
     operation is refused.
   - **Corrupt store:** has v2 tables or bindings and no marker. It is refused.
4. **The erasure journal.** A durable, append-only, per-store sequence written in the same transaction or operation
   as the `EncryptionKeyErasure` record by every v2 kernel. See acceptance criterion 3.

### 3. Storage, naming and write authority

1. **v2 storage has new names.** One container per sequence, constraint and mode, with the scope as a key column:
   SQL `{seq}_{constraint}_uc2` and `_uc2v`; MongoDB `{seq}+{constraint}+uc2` and `+uc2v`. Names are length-safe in
   the style of [decision 0006](0006-length-safe-sql-database-names.md) and recorded in a per-namespace binding with
   unique logical and physical indexes. Enumeration never parses names. Old binaries never touch v2 storage,
   because every v2 name is new.
2. **Keys.** SQL per-event-source tables use `PK NONCLUSTERED (Scope CHAR(32), EventSourceId NVARCHAR(450))` and
   `UNIQUE (Scope, Value CHAR(64))`; per-value tables use `PK (Scope, Value)`. `Scope` and `Value` are ASCII with a
   binary collation (`Latin1_General_BIN2`; PostgreSQL `COLLATE "C"`). MongoDB uses a compound `_id`
   (`{Scope, EventSourceId}` or `{Scope, Value}`) and a unique index on `{Scope, Value}`. **The SQL Server DDL is
   UNVERIFIED** until provider specs pass.
3. **Write authority.** One row per (namespace, sequence, constraint) holds `{Epoch, Phase, Revision}`. `Phase` is
   `Run(runId)` or `Live`. The store epoch is a fresh 128-bit value created at M2 and held only by kernels that read
   it from the marker. Every v2 mutation (`Save`, `Remove`, `RemoveValue`, `ClearValues`, `ClearAll`,
   `RemoveForSubject`) runs in one transaction that first matches the authority row on epoch, phase and revision,
   and aborts unless exactly one row matched. Any phase change updates the same row, so it conflicts with or waits
   for every in-flight write. Appends are serialized per sequence, so contention on the row is low.
4. **MongoDB needs transactions, so it needs a replica set.** A standalone server refuses the migration. Whether
   every supported Chronicle MongoDB deployment runs a replica set is **UNVERIFIED**.

### 4. Offline cutover and exclusion

The cutover is offline and operator driven. Code cannot exclude old binaries, so access does.

1. **Principals.** The v2 deployment uses a new storage principal P2, with credentials configured only in the v2
   deployment. Every principal any previous kernel used is P1; there may be several.
2. **Storage access of P1 is removed, per provider, and the kernel verifies it before M1.**

   | Provider | Removal | Verification before M1 |
   | --- | --- | --- |
   | PostgreSQL | `NOLOGIN`, `REVOKE CONNECT` from P1 and `PUBLIC` on every database, terminate P1 sessions | `rolcanlogin`, `has_database_privilege`, `pg_stat_activity`, other writers named in the attestation |
   | SQL Server, Azure SQL | disable the login or `DENY CONNECT SQL`; drop contained or Entra users or remove writer roles; `KILL` P1 sessions | `is_disabled`, role membership and permissions, `sys.dm_exec_sessions` |
   | MongoDB | `dropUser` or `revokeRolesFromUser` (delete the Atlas user); `killAllSessionsByPattern`; `killOp` | `usersInfo`, `$currentOp` with all users and idle connections (needs `clusterMonitor`), replica set present |
   | SQLite | stop every previous process; file owner and mode or ACL for the database, `-wal` and `-shm` files | owner and mode; **process exclusion beyond permissions is attested, not verified** |
   | InMemory | not applicable (process-local) | none |

3. **Attestation.** The v2 configuration carries `UniqueConstraintCutover { AttestationId, RevokedPrincipals[],
   NonKernelAdministrators[], AttestedBy, AttestedAt }`. The kernel runs every check in the table, refuses to
   continue if a check fails, the attestation is missing or the privilege to run a check is missing, and writes the
   attestation and every result into the marker. No check can be waived, and there is no option to accept residual
   risk.
4. **Exclusion is permanent.** P1 stays revoked. The checks run again before every step that changes state and
   before the contract drop. Re-granting P1 is unsupported, except in a backup-based downgrade (section 6).
5. **v1 storage is frozen by permissions after M4, and the description is exact.** PostgreSQL revokes
   `INSERT, UPDATE, DELETE, TRUNCATE` on every v1 table from everyone, including P2. SQL Server denies the same to
   `public`. **MongoDB's validator (`$expr: false`, strict, error) blocks inserts and updates but not deletes or
   drops**, so MongoDB v1 is frozen by access revocation, not by the validator. The validator is a second layer. The
   v2 kernel only reads v1, to classify collisions.
6. **Floor capability is defence in depth.** At M2 the kernel raises `UniqueConstraintEncodingV2` through the
   generation floor, so released floor-aware binaries (`v19.106.6` and later) fail at startup. They are still
   treated as unfenced. The membership probe and session listings are diagnostics only.

### 5. Migration protocol

**Durable state.** The marker holds `{Version, KeyId, Epoch, AttestationId, CheckResults, ErasureJournalCursor,
State}`, with `State` one of `V1`, `Fencing`, `Migrating`, `Verified`, `Contracted`. One manifest row per constraint
holds `{Generation, DefinitionRevision, RowState, RunId, TailAtStart, ProcessedThrough, CollisionSummary}`, with
`RowState` one of `Pending`, `Rebuilding`, `Verified`, `Blocked`, `Failed(reason)`, `Incompatible(reason)`.

**Driver.** A patch in the major release (`20.0.0/StartUniqueConstraintEncodingV2`) only checks that the
attestation exists and wakes a reconciler. The reconciler runs every step from the durable state, at each startup
and on a timer. Patch history is never consulted.

- **M0. Backup and verify.** The migration **refuses to start without a backup.** The backup is the unit in section
  6, and the kernel requires its recorded confirmation before it continues. Then it runs the provider checks of
  section 4. On failure the store stays `V1` and governed appends stay refused.
- **M1. Key.** Provision the key.
- **M2. Publish.** Write the marker with `State = Fencing`, `KeyId`, a fresh `Epoch` and the check results, and raise
  the floor capability. If floor advancement is refused, the migration stops at M2 with a typed reason and never
  bypasses the gate (see 5, floor advancement). Governed appends are refused from publication until `Verified`;
  the refusal is derived from the durable marker in `IsGovernedByPendingIndex`.
- **M3. Drain.** Cancel every non-terminal `ReindexConstraints` job and wait until job storage records it as
  terminal. Record `ErasureJournalCursor`. Set `State = Migrating`. Requests now carry `{Epoch, RunId}`, so a job
  that escapes the drain cannot pass the authority check.
- **M4. Prepare.** Create manifest rows, bindings, v2 containers and authority rows for every unique constraint on
  every sequence that has unique-constraint storage. Freeze v1 (section 4). Mark unsupported shapes `Incompatible`.
- **M5. Rebuild each row.** Set `Phase = Run(newRunId)` with a revision never used before; `ClearAll` under that
  authority; record `TailAtStart`; submit one job carrying `{Epoch, RunId}`. The handoff the job reports from
  `finally` is neutral evidence and carries no verdict. Reconciliation combines it with the job's durable terminal
  status for that run id:
  - **Succeeded**, with `ProcessedThrough >= TailAtStart`, the fold resolved, the same `DefinitionRevision` and no
    blocking collision: compare-and-set the phase from `Run(runId)` to `Live` and the row to `Verified`.
  - **Failed, cancelled, or progress unknown:** run M5 again with a new run id.
  - **Key unavailable or `MissingEncryptionKey`:** `Failed(reason)`, naming the sequence number.
  - **No terminal status yet:** if no job with that run id exists, start a new run.
- **M6. Cut over.** Run the section 4 checks again. Sweep the erasure journal from `ErasureJournalCursor` with
  `RemoveForSubject` on v2 under `Live` authority. Set `State = Verified` only when every row is `Verified`; governed
  appends are then admitted. Erasures from before M3 are covered by strict release in M5.
- **Contract (`State = Contracted`).** After `Verified`, and after the checks run again, drop **all** v1 containers,
  as a separate, explicit step. MongoDB containers are found by the legacy-name pattern. SQL tables are found by
  `UniqueConstraintTables.For`, including names PostgreSQL clipped, with catalog ownership confirmed first. Until
  the drop, `RemoveForSubject` also sweeps v1 on a best-effort basis. Pre-backfill v1 rows cannot be found by
  subject (decision 0013), which is why the drop is the default. Keeping v1 needs an approved privacy exception.

**Collisions caused by the migration are fixed in the data.** A duplicate that v2 refuses during M5 is never
acknowledged away and never skipped to reach `Verified`. The rule is in acceptance criterion 1. Remediation is one
of: revise or redact the events (the [#4905](https://github.com/Cratis/Chronicle/issues/4905) path), or change the
definition, which starts a new run. The report holds counts and keyed samples only, with no source ids and no values.

**Floor advancement.** `GenerationFloorAdvancement` combined with `UnknownGenerationMembershipProbe` refuses
advancement today, because the probe always reports non-uniform membership. The migration therefore needs an
executable path: an approved **offline floor-advancement path** (the operator attests the offline state, under the
same attestation and checks as section 4) or a real membership probe. Which one is built is **UNVERIFIED** and is
settled in the implementation. Refusal is handled in M2 as a typed stop. The gate is never bypassed silently.

### 6. Backup, downgrade and release

1. **The backup unit** is the key, marker, epoch, manifest, bindings, constraint tables and event log, backed up and
   restored together.
2. **The only supported downgrade is restoring a backup:** stop every node; restore the backup taken before M1;
   **reconcile compliance before reopening** by re-applying every erasure recorded after the backup, from the
   exported journal; restore P1's access, which is the only legitimate re-grant; start the previous release.
   Events appended after the backup are lost.
3. **Not supported:** a rebuild back to v1, restoring v1 tables alone, running a previous kernel against migrated
   data. A re-upgrade after a restore is a fresh M0 with a new epoch.
4. **Release level: major.** An offline minor is the fallback only if a major cannot be scheduled; the cutover is
   offline either way. This revises "Release: minor" on #4389.
5. **Release notes state** the outage, the new credentials (P2), the MongoDB replica-set requirement, and the
   data-loss window of a backup-based downgrade.
6. **Mixed versions.** A rolling upgrade is possible only up to M0. Until the store is `Verified`, a v2 kernel
   refuses governed appends, so there is no split-brain with v1 writers. From M0, previous binaries have no storage
   access. A v2 kernel without the current epoch cannot commit.

## Acceptance criteria

These are rules. v2 may not be called lossless, and may not be released, while any of them lacks a passing
specification.

1. **Collisions between different owners block cutover unless persisted evidence proves them safe.** When M5 finds
   two claims by different owners on one v2 value and scope, the row becomes `Blocked` and cutover is blocked. The
   only way past is trustworthy **persisted** evidence of historical ownership that shows the two claims were
   already one claim in v1. **Recomputing the v1 value is not evidence**: it cannot reproduce v1 (for example two
   open-schema GUID strings that differ only in case held distinct v1 claims, and MongoDB reconstructs both as
   `Guid`; culture-dependent `ToString()` defeats reconstruction in general). A holder or claimant that cannot be
   read counts as unsafe. Fixing collisions can lengthen the outage.
2. **The governed append's durable commit is fenced, and authority revisions are never reused.** The index-write
   fence alone is not enough, because the event commits first and the indexes update afterwards. A delayed append
   must not commit its event and only then find its authority stale. Validation and the durable commit of the event
   are tied to the same authority, and the commit fails when that authority is stale. Every transition of the
   authority row, including `Live` to `Run` to `Live`, gets a new revision that is never reused, so a request from
   before a rebuild cannot become valid again. How the fence is placed in each provider's append transaction is
   **UNVERIFIED** until specs prove it.
3. **The erasure journal carries the full erasure identity, is exported outside the databases, and is reconciled on
   restore.** Erasure is namespace-scoped, so each entry records `{Position, Namespace, KeyId, Subject, At}`, not
   only the subject. The journal is exported to storage outside the databases it protects, before any restore and on
   a schedule the release documentation states. After a restore, every journal entry newer than the backup is
   re-applied before the store reopens.
4. **Provider permissions are proven.** Provider specs against real PostgreSQL, SQL Server and MongoDB containers
   prove each removal and verification in section 4, including the privilege the kernel needs to run its checks, and
   prove the MongoDB behavior of the validator exactly as described. SQLite process exclusion remains an attestation.
5. **The SQL Server DDL is proven**, and the encoder vectors pass: a round trip for every format on every provider,
   append and replay yielding the same value.
6. **"Lossless" is claimed only after criteria 1 to 5 pass.** Release notes, documentation and issue comments do not
   use the word before then.

## Options considered

- **Offline, fenced cutover in a major release (chosen).** It is the only option where old writers cannot enforce a
  disjoint claim set, because their storage access is removed and v2 storage has new names.
- **Online or rolling migration with a bridge release.** Rejected. Released binaries carry no fencing code, the
  generation floor is checked only at startup, and the patching record is not proof of what runs. A dual-format
  matcher was already ruled out by step 2 of #4389.
- **Keep trailing-null padding and text-only type equivalence.** Rejected by the decider. It admits tuples v1
  refused and can stop a misaligned removal from releasing a claim. Unequal arity and mixed kinds are refused at
  registration instead.
- **Accept migration-caused merges by a durable per-constraint acknowledgement.** Rejected by the decider. Collisions
  are fixed in the data. No acknowledgement exists.
- **Classify collisions by recomputing v1 values.** Rejected after the round-3 review. It cannot prove the original
  equivalence (criterion 1).
- **Bridge downgrade to v1 by rebuild.** Not built. It would merge claims v2 keeps apart. Downgrade is from backup
  only.
- **Keep v1 rows after the cutover.** Rejected as the default. Pre-backfill rows cannot be found by subject, so they
  would outlive erasure. Retention needs an approved privacy exception.
- **Release as a minor.** The fallback only, and only if a major cannot be scheduled.
- **Mark an optional exclusion, or accept residual risk.** Removed. Exclusion is an obligation, never a waiver.

## Default if unanswered

The encoding stays v1. Null components lose their position, scopes alias, `-` stays ambiguous, PostgreSQL keeps clipping
scope-bearing names, and the unsalted hash still confirms guesses at unerased values. Every later fix to one of these
needs its own migration, with the same exclusion problem and no agreed protocol.

## Timeline and scope

- **Order of work.** This record comes first. No implementation and no v2 migration effect lands before it. The
  encoder, key and erasure work, storage and authority work, and the migration and contract are then built and
  verified in that order. The release is a major.
- **In scope.** The v2 value and scope encoding, the key, strict release and the erased status, the erasure journal,
  v2 storage in the in-memory, MongoDB, PostgreSQL, SQL Server and SQLite providers, write authority, the offline
  cutover with provider exclusion, the migration reconciler, the contract drop of v1, the backup requirement and
  the release notes, and updating `Documentation/constraints/declarative/unique.mdx` and
  `Documentation/constraints/model-bound/unique.mdx` for the null rule. The TypeScript in-process constraint code
  gets the v2 vectors.
- **Out of scope.** The delete-only patch for legacy rows
  ([#4866](https://github.com/Cratis/Chronicle/issues/4866)), which keeps its own approval. Patches 19.101.0 and
  19.112.0, which stay on v1. A bridge downgrade. Any change to the event log itself.

## Verification

**Done when:**

- the encoder, key, strict release, erasure journal, v2 storage, write authority, exclusion checks and reconciler
  exist in the tree;
- acceptance criteria 1 to 6 each have a passing specification, and the provider specs run in CI;
- the release notes contain the four statements in section 6;
- a store that was `V1` ends `Verified`, then `Contracted`, in a migration test per provider.

**Verify by:**

- `for_UniqueConstraintEncoder/when_encoding/`: `and_a_component_is_null`, `and_nulls_are_in_different_positions`,
  `and_a_value_contains_the_old_separator`, `and_a_float_literal_is_not_representable`,
  `and_an_unknown_string_looks_like_a_guid`, `and_the_value_is_an_array`, `and_casing_is_ignored`,
  `and_every_format_is_encoded` (vectors).
- `for_StoredConstraintEventContent/when_releasing/`: `and_a_legitimate_value_is_empty`,
  `and_the_subject_is_erased`, `and_the_key_is_missing_without_erasure`.
- `for_UniqueConstraintCutover/when_verifying/`: `and_an_old_principal_can_still_log_in`,
  `and_an_old_session_is_open`, `and_the_attestation_is_missing`, `and_the_check_privilege_is_missing`,
  `and_the_backup_is_missing`.
- `for_UniqueConstraintWriteAuthority/when_writing/`: `and_the_epoch_is_stale`, `and_the_run_was_superseded`,
  `and_clear_all_races_a_stale_write`, `and_a_delayed_append_commits_after_a_rebuild`,
  `and_a_revision_is_reused_after_live_run_live`.
- `for_UniqueConstraintEncodingReconciler/when_reconciling/`: `and_a_collision_has_different_owners`,
  `and_the_only_evidence_is_recomputed_v1`, `and_the_handoff_arrives_for_a_failed_run`,
  `and_the_kernel_crashed_after_submission`, `and_a_subject_was_erased_during_the_rebuild`,
  `and_the_key_is_unresolvable`.
- `for_UniqueConstraintErasureJournal/when_restoring/`: `and_the_entry_names_the_namespace_and_key`,
  `and_the_export_is_outside_the_database`, `and_entries_after_the_backup_are_reapplied`.
- Provider container specs: a round trip for every format, the v2 DDL, the freeze of v1 per provider including the
  MongoDB validator limits, and restore with compliance reconciliation.

## Consequences

- **Fixed.** Null components keep their position (JSON null and a missing property stay equivalent), `-` and `|` in values or scopes no longer alias, scopes no
  longer sit in names, and PostgreSQL clipping can no longer put two constraints in one table. The unsalted hash is
  gone, so guesses at unerased values are no longer confirmable without the store key.
- **Outage.** The cutover is offline. Its length depends on the rebuild and on any collisions that must be fixed in
  the data first.
- **New credentials.** Operators create P2 and remove P1. MongoDB requires a replica set and `clusterMonitor` for the
  checks.
- **Backup first.** No migration starts without a backup. Downgrade loses events appended after it, and depends on a
  correctly scoped erasure journal kept outside the databases.
- **Stricter registration.** Constraints with unequal arity, mixed kinds or unstable formats are refused, and
  existing ones are marked `Incompatible` until their definitions change.
- **Foreclosed.** Dual-format matching, a bridge downgrade, acknowledged merges, residual-risk waivers, and keeping
  v1 rows by default.
- **Unverified until proven.** Provider permissions and transactions, the SQL Server DDL, provider precision for
  temporal and numeric formats, the fence placement in each provider's append transaction, the floor-advancement
  mechanism, and whether all supported MongoDB deployments are replica sets.
