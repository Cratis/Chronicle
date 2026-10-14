---
id: 0006-length-safe-sql-database-names
title: Keep derived SQL database names within the provider's length limit and bind them once
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-13
decider: woksin
applies-to:
  - Source/Kernel/Storage.Sql/Database.cs
  - Source/Kernel/Storage.Sql/Cluster/**
  - Documentation/hosting/configuration/storage.mdx
---

<!-- markdownlint-disable-next-line MD025 -->
# Keep derived SQL database names within the provider's length limit and bind them once

Accepted on 2026-10-13 by woksin for Cratis/Chronicle#4849.

## Context

SQL storage gives every event store, namespace and read-model set its own database. `Database.ConnectionStringFor` names them by appending a suffix to the configured database name:

| Database | Name |
| --- | --- |
| Event store | `<cluster>+es+<store>` |
| Namespace | `<cluster>+es+<store>+<namespace>` (also for `Default`) |
| Read models, default namespace | `<cluster>+<store>` |
| Read models, other namespace | `<cluster>+<store>+<namespace>` |

Nothing limits the length. PostgreSQL silently truncates identifiers to 63 bytes, on `CREATE DATABASE` and on connect. When the configured name and the store name are long enough, an event store and its namespace truncate to the same string and share one database. The namespace migrations then fail with "already exists" on `NS-EventSeeds-v15_0_0`. `MigrateWithLock` swallows that error, and `EnsureMigratedOnce` records the namespace as migrated, so every later namespace migration is skipped. The first read of `FailedPartitions` fails with `42P01`. CI reproduced this deterministically (run 38437514540).

SQL Server limits database names to 128 characters and rejects longer ones, so it has no truncated databases today, but long names fail outright. SQLite uses file names and is out of scope here.

EF Core 10 (in use) takes a migration lock in `MigrateAsync` (`IMigrationsDatabaseLock`: a transactional history lock on PostgreSQL, an application lock on SQL Server), so two processes migrating the same database are serialized. Database creation happens before that lock, so `EnsureDatabaseExists` keeps tolerating a concurrent `CREATE DATABASE`. An "already exists" error from the migrations themselves means the schema and the migration history disagree, and only the history can say whether the database is complete.

The namespace database is reached by several independent paths: `Database.Namespace()`, the table accessors (`GetOrCreateTableDbContext`, which provision event-sequence and constraint tables without running the namespace migrations), read models, and jobs, which resolve their options synchronously through `IDatabase.GetJobsDbContextOptions` for an Orleans callback. None of them requires the event store database to be resolved first.

## Decision

1. **Length-safe names.** A derived name that fits the provider limit is used unchanged, so every existing deployment with short names keeps its databases byte for byte. The limits are 63 UTF-8 bytes on PostgreSQL (`NAMEDATALEN - 1`) and 128 UTF-16 code units on SQL Server (`sysname`). A longer name becomes a readable prefix plus a hash: the full logical name cut at a character boundary (never splitting a multibyte character or a surrogate pair) to leave room for the suffix, then `~` and the first 16 hex characters of the SHA-256 of the full logical name (UTF-8). Chronicle always connects with the final physical name, so neither provider truncates anything any more. The configured cluster database name is never rewritten; a configured name over the limit fails startup with a clear message instead of being truncated by the server.
2. **Bind every derived name once, in the cluster database.** A new cluster table, `DatabaseNames`, maps a logical identity (kind: event store, namespace or read models; store; namespace, with `Default` stored explicitly) to a physical name. It has a unique index on the logical identity and a unique index on the physical name. The physical-name index uses the provider's database-name equality: ordinal on PostgreSQL, the server's collation on SQL Server (on a case-insensitive server `Orders` and `orders` are one database). The configured cluster database is reserved in it as its own row. Short names are bound too, to themselves, so a name that is exactly at the limit owns its database before any longer name can truncate onto it. A binding never changes afterwards; a namespace created later can never move an existing database off its data.
3. **Decide ownership before binding.** For an unbound identity the candidate is its unchanged name when it fits, or for a long name the legacy name (the full name clipped the way PostgreSQL clipped it on `CREATE DATABASE`, `pg_mbcliplen`) when that database exists, and otherwise the hashed name. Before binding an unchanged or legacy candidate, the resolver lists its claimants: every identity derivable from the cluster's event store list and each store's namespace list, plus the identity being resolved, whose unchanged or clipped name equals the candidate under the provider's equality. The owner is the claimant that outranks all others: an event store outranks its own namespaces and read models, and a store's default-namespace read models outrank its named namespaces' read models. Unrelated claimants (two stores, two namespaces of one store, or the read models of a store called `es+<x>` against store `<x>`'s event store) have no order, so none of them is the owner. Only the owner binds the candidate; every other claimant goes to point 5. The binding is inserted. When the insert conflicts, the resolver reloads: if the identity is now bound (another kernel won the same race), that binding is used; if the physical name belongs to a different identity, the identity goes to point 5; a hashed-name conflict with a different identity fails loudly, since it means a SHA-256 prefix collision.
4. **Ownership order is enforced by the resolver, not by startup order.** Resolving a namespace or read-model identity first resolves its event store's identity; resolving a named namespace's read-model identity first resolves the default namespace's. With point 3, an event store keeps a legacy database it shares with its namespace whichever is reached first, and the namespace moves to its hashed name.
5. **Never move an identity away from its data silently.** A claimant that does not own the candidate is bound to its hashed name, with a warning, only when the candidate database holds no data that could belong to it. Otherwise resolving it throws `DatabaseOwnershipUnresolved`, naming the identity, the shared database and the other claimants, until an operator binds it. Data is judged by kind: for an event store, event-store tables or non-`NS-*` migration history; for a namespace, `NS-*` migration history, job tables, or namespace-only tables (event-sequence and constraint tables, `FailedPartitions`, observer state); for read models, any table that belongs to neither schema. The documentation gives the procedure: stop the kernels, create the target database, copy the identity's tables and their `__EFMigrationsHistory` rows, insert the binding row, restart. An operator-inserted binding is always honored.
6. **Bindings are read from the table, the memory copy is only a cache.** A namespace added at runtime is bound when it is created (`NamespaceStorage.Ensure`, asynchronous), on whichever kernel creates it, before anything else can reach it. On a cache miss the asynchronous paths reload from `DatabaseNames` and resolve. `GetJobsDbContextOptions` keeps its synchronous contract: on a miss it runs one synchronous indexed read of `DatabaseNames` and caches the result; if the identity is still unbound it throws, rather than computing a name the resolver has not checked. This covers a namespace created on another kernel and every access after a wipe cleared the cache.
7. **A migration that reports "already exists" with pending migrations is not success.** After `MigrateAsync` fails with "already exists", `MigrateWithLock` reads the pending migrations. If none are pending, the database is marked as migrated, as before. Otherwise it throws `DatabaseMigrationIncomplete`, naming the database and the first pending migration, and the database is not marked. This proves the history is complete, not that every object exists; it is the same guarantee every other migrated database has.
8. **The development wipe keeps the layout.** `Wipe` (DEVELOPMENT only) empties tables rather than dropping databases. It keeps `DatabaseNames` (like `__EFMigrationsHistory`), reaches every bound database, and clears the in-memory bindings, the migration caches and the cached jobs storage on every provider, not only SQLite.

## Options considered

- **Hash every derived name.** It would move every existing deployment to new, empty databases. Not taken.
- **Bind only long names.** A short name exactly at the limit stays unprotected, so a longer namespace name can truncate onto it and reproduce the collision. Not taken.
- **Recompute the legacy decision from the store and namespace lists on every start.** A namespace created after an upgrade can turn a previously unique legacy name into a shared one, so a later start would move the existing namespace to an empty database. Not taken.
- **First claim wins.** Whichever kernel or identity resolves first would own a shared legacy database, which depends on activation order and can hand one store's data to another. Not taken.
- **Log and continue with an empty database.** Writes would land in a fresh database while history sits in the old one, giving two diverging histories. Not taken.
- **Fail startup when names are too long.** Safe, but it breaks deployments that work today because their truncations do not collide. Not taken, except for the configured cluster name.

## Default if unanswered

Implement as decided above, as a patch release, keeping every public synchronous contract (`IDatabase.GetJobsDbContextOptions`) unchanged. If that cannot be kept, the change becomes minor.

## Verification

All storage specs use real `Database` instances on the container fixtures, not substituted `IDatabase`.

- Name function: a name at the limit is unchanged; one byte (PostgreSQL) or one code unit (SQL Server) over gets prefix plus hash within the limit; multibyte characters and surrogate pairs are never split; two names sharing a 63-byte prefix get different names.
- PostgreSQL: a long store and namespace get distinct databases and every namespace migration runs; a store name of exactly 63 bytes keeps its database when its namespace is resolved first; an existing legacy database is kept by the store, and the namespace gets the hashed name with a warning when the shared database holds no namespace data, or throws `DatabaseOwnershipUnresolved` when it does; two existing stores truncated onto one database with data both throw; an operator-inserted binding is honored; two `Database` instances racing to bind the same identity agree; a namespace created on one instance is reachable through jobs on another; bindings survive a restart; jobs and table accessors reached before `Namespace()` use the same physical name.
- SQL Server: a case-insensitive server rejects a second identity binding a name that differs only in case.
- Migrator: "already exists" with pending migrations throws `DatabaseMigrationIncomplete` and leaves the database unmarked; with none pending it is marked.
- SQL Server: a name longer than 128 code units is created under its hashed name.
- Wipe keeps bindings and resets jobs storage.
- The out-of-process PostgreSQL transport integration spec goes back to its long store name.

## Consequences

- New deployments with long names get readable, distinct database names on both providers.
- Existing deployments with short names see no change apart from one binding row per database. Existing deployments with long names keep their database where ownership is clear, and stop with a named, documented manual step where namespace data would otherwise be stranded.
- The cluster database gains one table and one migration, and every database access goes through a binding.
- The length-independent collision between the read-model database of a store called `es+<x>` and the event store database of store `<x>` is now detected by the same binding rules instead of silently sharing a database.
