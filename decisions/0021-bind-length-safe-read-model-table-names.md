---
id: 0021-bind-length-safe-read-model-table-names
title: Bind length-safe read model table names to their scoped logical owners
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-11-01
applies-to:
  - Source/Kernel/Storage.Sql/Database.ReadModelTable*.cs
  - Source/Kernel/Storage.Sql/Cluster/DatabaseLocation.cs
  - Source/Kernel/Storage.Sql/Cluster/DatabaseNameResolver.cs
  - Source/Kernel/Storage.Sql/Cluster/SqlDatabaseCatalog.cs
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/ReadModels/**
  - Source/Kernel/Storage.Sql/Sinks/**
  - Documentation/sinks/index.mdx
---

<!-- markdownlint-disable-next-line MD025 -->
# Bind length-safe read model table names to their scoped logical owners

Proposed for [#4340](https://github.com/Cratis/Chronicle/issues/4340). This extends the naming discipline of [0006](0006-length-safe-sql-database-names.md) to tables; it does not replace that database-naming contract.

## Context

PostgreSQL truncates table identifiers to 63 UTF-8 bytes, and SQL Server rejects identifiers longer than 128 UTF-16 code units. Adding a replay prefix or an occurrence identifier can cross the limit even when the live container fits. Two distinct logical names must not address the same table, and an upgrade must not replace an existing table with an empty one under a new name.

## Decision

1. Keep short names unchanged; use a character-boundary-safe prefix and `~` plus 16 hexadecimal SHA-256 characters of the complete logical name for longer names. SQLite retains its names.
2. Persist a binding in the read-model database, scoped by event store, namespace, read model identifier, original container, table role, and full logical name. Live, replay, and each retained backup have distinct identities. A physical name remains reserved after removal.
3. Adopt legacy tables only with unambiguous persisted ownership and a verifiable table shape. Never infer ownership from activation order, truncated-name equality alone, or an inaccessible metadata scope. A Ready binding is stable; a missing Ready table is an error, never permission to silently allocate a replacement.
4. Discover possible owners read-only. Database location is Found, Absent, or Unknown. Absent requires positive visibility: a successful SQLite directory listing, PostgreSQL catalog visibility without row security, or the documented SQL Server catalog permissions. Unknown blocks reservation and adoption, but not existing Ready bindings.
5. Cache successful resolutions by logical handle and credential-free physical placement. A warm open resolves its target once, without repeating ownership enumeration or ledger queries. Never cache a failure.
6. Commit replay renames, backup readiness, and replay-attempt cleanup with compare-and-set ledger updates in the same database transaction. Retention removes the physical table and tombstones its binding without releasing its name to a different owner.

## Alternatives

- **Truncate every identifier:** preserves the collision that loses isolation between logical owners.
- **Always hash, without bindings:** redirects existing legacy data and cannot prove ownership on upgrade.
- **Treat an unsuccessful catalog lookup as absence:** confuses restricted visibility with no owner and can adopt someone else's table.
- **Require every dormant scope to be bound first:** blocks ordinary upgrades indefinitely. Read-only discovery can establish absence without opening or registering those scopes.

## Default, scope, and timeline

Until this proposal is accepted, the existing released behavior remains the default, including its long-name collision risk. The proposal is intended to hold across upgrades and replay retention, not just one deployment. It covers SQL read-model tables and their ledger, not event-sequence tables, MongoDB collections, or changes to `IDatabase`'s public interface. PostgreSQL and SQL Server timing claims require provider-specific measurement; SQLite measurements cannot establish them.

## Verification

**Done when:** long live, replay, and backup names remain distinct; existing bindings and data survive reopen and replay; ambiguity and Unknown visibility cannot create or adopt a table; Ready bindings remain usable; replay failure rolls back DDL and ledger state together.

**Verify by:** run the resolver, accessor, Database, SQLite sink and replay specifications; compile provider specifications in Release with warnings as errors and run them in provider CI; compare warm Database and Sink allocations and latency, cold resolution, and target/query counts against the pre-change baseline. A failing or unavailable gate is not a verified decision.
