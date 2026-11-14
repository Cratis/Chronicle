---
title: Storage compatibility in Cratis Chronicle
description: Choose a supported Chronicle storage backend, understand the measured DocumentDB limitations, and check a MongoDB-compatible service before using it.
---
Choose a supported storage backend before deploying Chronicle. A service that exposes a MongoDB API does not necessarily implement the features Chronicle and its consumers require.

## Supported storage

Chronicle supports MongoDB, SQL Server, PostgreSQL, and SQLite. MongoDB requires a replica set for transactions and change streams. Use [storage configuration](configuration/storage.mdx) for the authoritative backend list, configuration, and deployment limitations, including the ephemeral in-memory backend.

## Azure DocumentDB and MongoDB-compatible services

DocumentDB is **not supported** as a Chronicle storage backend. The measured local build accepted event storage operations but lacked change streams and index collation, both required by Chronicle's MongoDB storage path.

:::caution[Managed Azure DocumentDB is unverified]
These measurements apply only to the local open-source DocumentDB build. The managed Azure DocumentDB service has not been tested; its change-stream and collation support may differ. MongoDB API compatibility alone is not evidence of Chronicle compatibility.
:::

The measurements recorded in [Chronicle issue #5642](https://github.com/Cratis/Chronicle/issues/5642) used `ghcr.io/documentdb/documentdb/documentdb-local` with MongoDB API 7.0, Chronicle 19.122.0, Arc 22.163.0, and MongoDB.Driver 3.13.0. All data was fictional.

| Feature | Result | Note |
| --- | --- | --- |
| Event appends and multi-document transactions | Works | Duplicate keys were rejected; aborted writes were absent. |
| Projections to read models | Works | Projections produced read models. |
| Unique constraints | Works | Two concurrent appends produced one winner. |
| Persisted reads after kernel restart and kernel readiness | Works | Stored data remained readable after restart and the kernel became ready. |
| Collection-level and database-level `$changeStream` | Fails | `Stage $changeStream is not supported yet in native pipeline`; breaks Arc MongoDB observable queries and read-model container watchers. |
| Index collation, even `locale: simple` | Fails | `createIndex.collation is not implemented yet`; prevents some kernel indexes, such as the open-incident gauge index, and breaks sink index reconciliation. |
| Chronicle MongoDB storage specs against the container | 22 passed, 6 failed | Failures came from the two feature gaps above; two additional cleanup `NullReferenceException` errors occurred. |

The kernel also logged `ObjectDisposedException` during shutdown/restart. That observation was not attributed to DocumentDB.

## Startup capability checks

When starting with MongoDB storage, Chronicle probes database-level change streams and collation once, outside the append path. An explicit unsupported-feature response stops startup with a message naming the missing features and linking to this page. Permissions, connectivity, timeouts, and standalone-server errors make a probe inconclusive: Chronicle warns and continues rather than labeling the service unsupported. A warning does not establish that a standalone server can run Chronicle; MongoDB still requires a replica set.

In `Migrate` schema mode, the collation probe creates an index with `Collation.Simple` on a temporary `chronicle_capability_probe` collection in the system database, then drops the collection and its index. It does not remove a collection that already existed. Cleanup failures produce a warning; remove a leftover probe collection with a schema identity before retrying.

In `Verify` mode, the probe performs a read-only find with `Collation.Simple` instead of DDL. This is best effort: a service may accept collation on a find but reject it when creating indexes, so a successful read probe does not establish index-collation support.

## Checking another service

Use a non-production endpoint you control. Set `CHRONICLE_MONGODB_CONNECTION_DETAILS` to its connection string and run only the MongoDB storage spec project:

```bash
export CHRONICLE_MONGODB_CONNECTION_DETAILS="mongodb://mongo.example.test:27017/?replicaSet=myReplicaSet"
dotnet test Source/Kernel/Storage.MongoDB.Specs/Storage.MongoDB.Specs.csproj --configuration Release
```

Follow [checking a MongoDB-compatible service](../contributing/integration-tests.md#checking-a-mongodb-compatible-service) for permissions and recording results per feature. Change-stream or collation spec failures mean the service lacks required support. Never commit real connection details. Someone with access must run this manual check against the managed Azure service; CI does not verify that service.
