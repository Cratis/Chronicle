---
id: 0005-settle-unique-constraint-claims-in-every-store
title: Settle unique-constraint claims in the store, in every provider
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-13
decider: woksin
applies-to:
  - Source/Kernel/Storage.InMemory/Events/Constraints/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/UniqueConstraints/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/UniqueConstraintDbContext.cs
  - Source/Kernel/Storage.MongoDB/Events/Constraints/**
  - Source/Kernel/Core/Events/Constraints/**
  - Source/Kernel/Storage/Events/Constraints/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Settle unique-constraint claims in the store, in every provider

Accepted on 2026-10-13 by woksin for Cratis/Chronicle#3744, with the changes recorded below.

## Context

Issue #3744 was filed as "unique constraints are only enforced on MongoDB". That is wrong, and the reporter corrected it: all three providers check `IUniqueConstraintsStorage.IsAllowed`, and the append path does not race, because the `EventSequence` grain is non-reentrant and awaits the index write inside its turn (`EventSequence.CompleteDurableAppend`). What differs is whether the *store* settles a claim when it is saved. MongoDB does, through a unique index whose duplicate-key error becomes `DuplicateUniqueConstraintValue`. SQL and InMemory do not, in the default (per-event-source) mode.

The exposure is narrow but real:

- **Reindex alongside live appends.** Adding or changing a unique constraint always triggers a reindex (`Constraints.cs`). `ReindexConstraintsStep` runs outside the sequence grain. It removes a source's claim and later replays `Save`. If a live append lets another source claim the value in between, SQL and InMemory keep both claims permanently.
- **Stale claims across a reindex.** In default mode the step clears a source's old claim only when it first sees that source in the replay. A historical claim by one source can therefore meet a stale claim by another that has not been seen yet (for example after toggling `IgnoreCasing`). Once claims are settled, the historical claim is rejected for good. MongoDB already has this defect today.
- **SQL multi-activation.** Two activations of the same sequence grain during silo handover can both pass `IsAllowed` before either writes. Only SQL can lose here; the in-memory store is single-process.

The contract is also broken, independent of any race. `DuplicateUniqueConstraintValue` states that every `IUniqueConstraintsStorage` implementation reports a violated claim this way. Calling `Save` twice for two sources with the same value reproduces it deterministically: SQL and InMemory raise nothing, and releasing the first holder does not free the value because the second row survives. `Chronicle.Testing` runs on InMemory, so a spec can pass there and fail on MongoDB.

SQL's schema is the obstacle to the obvious fix. The per-event-source table has a non-unique index on `Value` (`UniqueConstraintDbContext`, `UniqueConstraintMigrator.CreateTable`) and no index at all on SQL Server, because `Value` was widened to unbounded for long e-mail addresses (`0f5bfdce1`, 2026-06-07) and `NVARCHAR(MAX)` cannot be indexed. Values have since become a 64-character lowercase SHA-256 hex hash (`UniqueConstraintDefinitionExtensions.GetValue`, `8fca2227e3`, 2026-06-29), so the unbounded column is vestigial. Per-value tables already use `Value` as a primary key at length 200.

This crosses every storage provider, changes a persisted schema and reverses part of an earlier commit, so it needs a durable decision rather than a code-review ruling. [Decision 0002](0002-event-generation-preservation-and-delivery.md) and [Decision 0004](0004-ordered-lanes-observer-delivery.md) are unaffected.

## Decision

Every storage provider settles a unique-constraint claim itself, when the claim is saved, and reports a lost claim as `DuplicateUniqueConstraintValue`. The kernel reindex tolerates duplicate history. `DuplicateUniqueConstraintValue` stays diagnostic: the event is already durable when the index is written, so the append is not failed.

1. **InMemory** keeps an owner map keyed by (constraint, scope, value) next to the per-source index and changes both under one lock. `Save` throws when another source owns the value and leaves the existing claim untouched; the same source reclaiming is a no-op; moving to a new value releases the old one. `Remove` drops both entries and `IsAllowed` is a direct lookup.
2. **SQL** puts a unique index on `Value` on every provider, named `IX_<table>_Value`, and bounds `Value` to 200 characters on SQL Server (the length per-value tables use), replacing the unbounded column. The bounding runs only behind a guard that never truncates: the guard counts rows whose value is longer than 200 characters and runs that length check and the `ALTER` in one transaction, so a value cannot slip in between. If any such row exists the narrowing is skipped and a warning is logged. Legacy pre-hash rows block narrowing; in that case the warning names the legacy-rows issue. `EnsureValueColumnIsUnbounded` is replaced by an `EnsureValueIndexIsUnique` step, because leaving it in place would widen the column again and drop the index on SQL Server. PostgreSQL and SQLite keep `TEXT`, which can already be indexed.
3. **SQL violation translation.** `SavePerEventSource` catches the unique violation through the existing `IsUniqueViolation`, clears the change tracker and decides from the data rather than the error text. If a different source owns the value it throws `DuplicateUniqueConstraintValue`; the failed update rolls back, so the previous claim survives. If the same source won a primary-key insert race, it retries as an update.
4. **A rejected value move keeps the old claim**, as MongoDB does today. A source that tries to move onto a value another source holds stays where it was.
5. **Kernel reindex.** In default mode `ReindexConstraintsStep` clears the whole scope at the first event it sees for that scope, as per-value mode already does with `ClearValues`; `ClearValues` and its documentation are extended to cover default mode. The step catches `DuplicateUniqueConstraintValue` per event, logs it and continues. When history contains duplicates, **the first claimant in log order wins**, which is what an append would have decided. A live append during the reindex window can beat a historical claimant, so "first in log order" holds for history only. This changes MongoDB's reindex, which today fails on the first duplicate and leaves the index half built.
6. **One policy for existing duplicates, shared by all providers.** When the unique index cannot be created because the table already holds duplicates, fall back to a non-unique index (none on SQL Server if the column could not be narrowed), log a warning with the table name and the number of duplicate values, delete nothing, and try again at the next process start. The warning is logged once per table per process. On SQL Server, case-insensitive collations can surface duplicates that differ only by case. An operator removes the losing rows or runs a reindex once item 5 has shipped. This is MongoDB's `EnsureIndex` behavior (`when_duplicates_already_exist`), adopted for SQL.

## Options considered

- **Fail at first use when duplicates exist.** SQL tables are upgraded lazily on first use, so there is no startup hook. Failing there breaks every check against that constraint. Not taken.
- **Auto-resolve duplicates by keeping the lowest sequence number.** It deletes data without an operator and picks a winner nobody confirmed. Not taken.
- **Fall back to a non-unique index, warn, retry later.** Taken. It mirrors MongoDB, it answers the reporter's concern that the migration fails on existing deployments, and it loses nothing.
- **SQL Server computed persisted `HASHBYTES` column with an index.** The model and the schema would differ per provider, for no gain when hashed values already fit in 200. Not taken.
- **Leave SQL Server unindexed.** It leaves the provider most used for multiple silos without a settled claim. Not taken.
- **Narrow `Value` to 64.** It ties the schema to the current hash format and rejects more legacy rows than 200 does. Not taken.
- **Serialized check-then-insert with row locks.** On SQL it still needs the unique index to be correct across silos, so it adds a mechanism without removing one. Not taken.
- **Reindex into a shadow table and swap.** This is the real cure for a reindex racing live appends, but it is far larger. Deferred as a possible follow-up; this decision settles claims in the store instead.
- **Fail the append on a lost claim.** Settled earlier and unchanged: the event is durable before the index write.

## Default if unanswered

Nothing ships. SQL and InMemory keep accepting duplicate holders in default mode, so `DuplicateUniqueConstraintValue` stays a promise only MongoDB keeps, releasing one holder still leaves the value claimed, and `Chronicle.Testing` keeps diverging from MongoDB. MongoDB's reindex keeps failing on duplicate history and on stale claims. The cost is quiet: a reindex racing a live append, or two grain activations on SQL, can leave two holders of one value with no signal until someone releases one.

## Timeline and scope

Implementation order: kernel reindex fix, then InMemory, then SQL (including a SQL Server integration run). Delivery is three patch PRs, in this order:

1. **Kernel reindex fix** (item 5). It also repairs MongoDB, and it must land first so a settled store never meets a reindex that fails on the first duplicate.
2. **InMemory owner map** (item 1).
3. **SQL schema and migration** (items 2, 3, 4 and 6). This carries the risk. It needs the full cross-provider integration run, including SQL Server, before it merges.

In scope: `IUniqueConstraintsStorage` implementations for InMemory, SQL and MongoDB; `ReindexConstraintsStep`; `ClearValues`; the SQL model, migrator and storage.

Out of scope:

- `IUniqueEventTypesConstraintsStorage`, which derives uniqueness from the event sequence and has no index to make unique.
- Rows written before values were hashed (before `8fca2227e3`). They can never match a hashed lookup, so they no longer block anything. They are tracked in a separate issue.
- A shadow-table reindex, and an operator alert in addition to the log warning.
- Changing `DuplicateUniqueConstraintValue` from diagnostic to rejecting.

### Resolutions

The decider accepted all four questions with the refinements folded into the items above.

- (a) SQL Server `Value` is narrowed to `NVARCHAR(200)` behind the never-truncate guard (item 2). This partly reverses `0f5bfdce1`.
- (b) The fall-back-and-warn policy is shared by all providers (item 6).
- (c) A rejected value move keeps the old claim, as MongoDB does (item 4).
- (d) "First claimant in log order wins" is the reindex rule for duplicate history, which changes MongoDB's reindex (item 5).

## Verification

**Done when:** in every provider a second event source cannot hold a value that another holds, and the attempt reports `DuplicateUniqueConstraintValue` with the first holder intact; releasing the holder frees the value; a reindex over duplicate or stale-claim history completes instead of failing, with the first claimant in log order holding each value; and upgrading a SQL table that already holds duplicates keeps every row, logs the table and duplicate count, and retries at the next process start.

**Verify by:**

- InMemory specs under `for_UniqueConstraintsStorage/when_saving`: `and_two_sources_claim_the_same_value` (throws, the first source stays sole holder), `and_the_holder_releases_after_a_rejected_claim`, `and_a_source_moves_onto_a_held_value` (old claim kept), and a concurrent-claims spec using `Task.WhenAll`. The existing `and_the_holder_has_moved_on_to_another_value` stays green. Each is red before the change.
- The same cases against the SQLite fixture in `Storage.Sql.Specs` (`given/a_unique_constraints_storage.cs`), changed to create tables through the real `UniqueConstraintMigrator`.
- SQL migrator specs on real databases, using the PostgreSQL fixture in `Storage.Sql.Specs` and the SQL Server fixture under `Integration/Client`: a legacy-shaped table (unbounded column, non-unique index) is upgraded to a unique index; a table with duplicates falls back, keeps every row and warns once per process; a SQL Server value longer than 200 characters is skipped, not truncated, and legacy pre-hash rows block narrowing with a warning that names the legacy-rows issue; a second run changes nothing.
- Kernel specs `for_ReindexConstraintsStep/when_reindexing_duplicate_history` (does not fail, first claimant wins) and `when_a_stale_claim_shares_a_replayed_value`, on the real InMemory storage.
- The cross-provider integration run, including SQL Server, is green on the SQL PR.

## Consequences

The store, not the caller, is the authority on who holds a value, in every provider, so `Chronicle.Testing` matches production and the `DuplicateUniqueConstraintValue` contract is true. A reindex survives history that the append path would have refused.

The cost is a schema change on SQL: a unique index everywhere, and SQL Server `Value` reduced from unbounded to 200 where the guard allows it. Tables that already hold duplicates stay on a non-unique index until an operator reconciles them, and stay unsettled until then. The first-claimant rule means the later claimant in a duplicate history loses its claim silently apart from a log line. A reindex racing live appends is narrowed, not eliminated, until a shadow-table reindex exists. Reverting the SQL Server narrowing would mean widening the column again and giving up its index.
