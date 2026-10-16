<!-- cratis-ai-managed: skills/cratis-chronicle-event-constraints/references/storage-uniqueness.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Unique claims across storage providers

Requires Chronicle 19.59.7 for SQL unique claims to be settled by database unique
indexes and for rejected moves to preserve the old claim.
Requires Chronicle 19.59.3 for cross-source claim enforcement, rejected-move preservation and release in memory.
Check the kernel image/storage package versions; earlier storage can allow a
racing second holder or lose the old claim on a rejected move.

On PostgreSQL, SQL Server and SQLite, per-event-source constraint tables have a
unique value index. SQL startup upgrades existing tables without dropping data.
Duplicate rows retain a non-unique index and produce a warning; remove duplicates
only with authorization, then restart to obtain the unique index. SQL Server
bounds the value column to 200 characters but skips that upgrade when long or
legacy unhashed values would be truncated. A warning is not a successful index
upgrade; inspect the table before relying on atomic claims.

Requires Chronicle 19.59.1 for reindexing histories containing duplicates to
complete: the first claimant in event-log order retains the value, later duplicate
claims are logged and skipped. Reindexing does not retract their stored events.
Older kernels can fail the reindex instead.

Run the affected specifications with `dotnet test`, asserting a second source's
rejection, unchanged original claim after a failed move, and successful reuse
after release. Real SQL integration must prove two competing claims cannot both
succeed; an in-memory pass does not prove the SQL index was upgraded.
