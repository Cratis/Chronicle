---
title: MongoDB permissions
description: The databases the Chronicle kernel uses, including the System databases, and the MongoDB roles and privileges it needs in each.
---
Chronicle fails with `not authorized on System to execute command { listIndexes: "EventTypeStatistics" ... }` when its MongoDB credential is scoped to your application's databases only. The kernel has its own event store, named `System`, that always exists. It uses three databases that nobody configures, and the credential needs access to them as well as to your event stores.

This page lists every database Chronicle uses, what it does in each, and which MongoDB roles cover them.

## Databases Chronicle uses

In the table, `P` is the value of `storage.databaseNamePrefix`. It is empty by default. See [Storage](storage.mdx#database-name-prefix).

| Database | Name | Holds |
| --- | --- | --- |
| Cluster | `P` + `chronicle+main` | Cluster-wide state, Orleans clustering and reminders |
| Event store | `P<store>+es` | State shared by all namespaces of an event store, such as reactor definitions |
| Namespace | `P<store>+es+<namespace>` | Event sequences, observer state, jobs and constraints of one namespace |
| Read models, `Default` namespace | `P<store>` | Read models materialized for the `Default` namespace |
| Read models, other namespaces | `P<store>+<namespace>` | Read models materialized for that namespace |
| Global confidentiality keys | `P!chronicle-encrypted!+es+!chronicle-encrypted!` | Only when encryption keys use global scope |

### The System databases

The kernel's own event store is named `System`. Chronicle registers its event types, ensures its `Default` namespace and registers its reactors at every startup, so these three databases always exist:

- `System`: read models of the kernel, for example the event type statistics.
- `System+es`
- `System+es+Default`

With a prefix, each name starts with the prefix. With `chr_`, they are `chr_System`, `chr_System+es` and `chr_System+es+Default`.

### Worked example

For an event store `Orders` with the namespaces `Default` and `tenant-a`:

| Without a prefix | With prefix `chr_` |
| --- | --- |
| `chronicle+main` | `chr_chronicle+main` |
| `System` | `chr_System` |
| `System+es` | `chr_System+es` |
| `System+es+Default` | `chr_System+es+Default` |
| `Orders` | `chr_Orders` |
| `Orders+es` | `chr_Orders+es` |
| `Orders+es+Default` | `chr_Orders+es+Default` |
| `Orders+tenant-a` | `chr_Orders+tenant-a` |
| `Orders+es+tenant-a` | `chr_Orders+es+tenant-a` |

Creating an event store or a namespace at runtime adds databases to this list. The prefix applies to every derived name, including the cluster database. It does not change event store or namespace names in the API, collection names, or a database name returned by [storage connection resolution](storage-connection-resolution.md), which is used as is. Chronicle never touches `admin`, `local` or `config`.

## What Chronicle does in each database

| Operation | Cluster | Event store | Namespace | Read models |
| --- | --- | --- | --- | --- |
| find, insert, update, delete | Yes | Yes | Yes | Yes |
| aggregate (including `$lookup` in the same database) | | Yes | Yes | |
| listIndexes, createIndexes, dropIndexes | Yes | Yes | Yes | Yes |
| listCollections | | | Yes | Yes |
| createCollection | | | | Yes |
| dropCollection | | | | Yes |
| renameCollection (same database only) | | | | Yes |
| Change streams (database-level watch) | Yes | | | Yes |
| Sessions and multi-document transactions | | Yes | Yes | Yes |
| dropDatabase | | | Only when a namespace is removed | |

Also:

- **Cluster-level `listDatabases`**: event store discovery and the MongoDB health check list database names. Without the `listDatabases` action, MongoDB 4.0.5 and later returns only the databases the user has privileges on. Discovery and the health check still work, but see only the databases you granted.
- **Not used**: `$out`, `$merge`, `collMod`, cross-database rename, and commands other than `hello` run through `runCommand`.
- **Development only**: the kernel reset in the development tools drops databases and collections and creates a `_ready_probe` collection. Production credentials do not need that.

## Recommended roles

In order of preference.

### 1. `readWriteAnyDatabase` on a server dedicated to Chronicle

This is the simplest option and it survives new event stores and namespaces. MongoDB cannot grant privileges by name prefix, so per-database grants must be extended every time a database is added. `readWriteAnyDatabase` covers every database except `local` and `config`, and includes cluster-wide `listDatabases`.

Add `dbAdmin` on a namespace database, or a custom `dropDatabase` grant, only if you remove namespaces: `readWrite` and `readWriteAnyDatabase` do not include `dropDatabase`.

### 2. `readWrite` per database on a shared server

Grant `readWrite` on every database in [the list above](#databases-chronicle-uses), including the three System databases. `readWrite` covers find, insert, update, remove, createCollection, dropCollection, createIndex, dropIndex, listIndexes, listCollections, renameCollectionSameDB, changeStream and killCursors.

Add the grants before you create an event store or namespace, or Chronicle fails on its first access to the new database. Optionally add a custom role with the cluster-level `listDatabases` action so event store discovery sees every Chronicle database.

An illustrative `mongosh` example for the `Orders` event store with prefix `chr_`:

```javascript
use admin
db.createUser({
  user: "chronicle",
  pwd: passwordPrompt(),
  roles: [
    { role: "readWrite", db: "chr_chronicle+main" },
    { role: "readWrite", db: "chr_System" },
    { role: "readWrite", db: "chr_System+es" },
    { role: "readWrite", db: "chr_System+es+Default" },
    { role: "readWrite", db: "chr_Orders" },
    { role: "readWrite", db: "chr_Orders+es" },
    { role: "readWrite", db: "chr_Orders+es+Default" },
    { role: "readWrite", db: "chr_Orders+tenant-a" },
    { role: "readWrite", db: "chr_Orders+es+tenant-a" }
  ]
})
```

### 3. One identity per kind of database

With [storage connection resolution](storage-connection-resolution.md), a resolver can return a different identity for each kind of database: cluster, event store, namespace and read models. Each identity needs the privileges for its own kind of database, as listed in the matrix above.

## A minimal custom role

A custom role can grant only the actions in the matrix. It is per-database, so it has the same maintenance cost as `readWrite`: you add a privilege for every new event store and namespace. It is an illustrative `mongosh` example, derived from the operations above. The actions are checked against the MongoDB manual, but the role has not been validated against a running Chronicle with authentication enabled. Use `readWriteAnyDatabase` when in doubt.

```javascript
use admin
db.createRole({
  role: "chronicleKernel",
  privileges: [
    {
      resource: { db: "chr_System", collection: "" },
      actions: [
        "find", "insert", "update", "remove",
        "createCollection", "dropCollection",
        "createIndex", "dropIndex", "listIndexes", "listCollections",
        "renameCollectionSameDB", "changeStream", "killCursors"
      ]
    }
    // Repeat for every database in the list, including the other System databases.
    // Optional, only when you remove namespaces:
    // { resource: { db: "chr_Orders+es+tenant-a", collection: "" }, actions: ["dropDatabase"] }
    // Optional, so event store discovery sees every database:
    // { resource: { cluster: true }, actions: ["listDatabases"] }
  ],
  roles: []
})
```

## Replica set requirement

Change streams, used for live read model queries and event store observation, and multi-document transactions, used for appends, reactor definitions and generation fencing, need a replica set or a sharded cluster. A standalone `mongod` is not enough. See [Storage requirements](../production.md#storage-requirements).

## Managed tiers and the System databases

Some managed tiers restrict which databases a user can be granted. A reported case on MongoDB Atlas Flex is that a user could not be granted access to a database named `System`. This is reported behaviour; it is not confirmed against the Atlas documentation, and Atlas Flex support for custom roles, database-specific grants and change streams has not been verified here.

The workaround is to set `storage.databaseNamePrefix` (environment variable `Cratis__Chronicle__Storage__DatabaseNamePrefix`) before the first start. Every database, including `System`, `System+es` and `System+es+Default`, then gets a prefixed name such as `chr_System`, which can be granted like any other. The prefix does not change names in the API, collection names or names returned by connection resolution.

:::caution
Changing the prefix later points Chronicle at different, empty databases. It does not rename or migrate data. Decide the prefix before the first start. See [Database-name prefix](storage.mdx#database-name-prefix).
:::

Connection resolution's `database` override is the other way to control where a database is placed. See [Storage Connection Resolution](storage-connection-resolution.md). It has not been verified on Atlas Flex.

## Troubleshooting authorization errors

MongoDB reports a missing privilege as:

```text
not authorized on <db> to execute command { <command>: ... }
```

1. Find `<db>` in [the list of databases](#databases-chronicle-uses). A name such as `System` or `System+es+Default` is a kernel database, not one of yours.
2. Map `<command>` to the action it needs:

   | Command | Action |
   | --- | --- |
   | `find`, `insert`, `update`, `delete` | `find`, `insert`, `update`, `remove` |
   | `aggregate` | `find` |
   | `aggregate` with `$changeStream` | `changeStream` |
   | `listIndexes` | `listIndexes` |
   | `createIndexes` | `createIndex` |
   | `listCollections` | `listCollections` |
   | `renameCollection` | `renameCollectionSameDB` |
   | `dropDatabase` | `dropDatabase` |
   | `listDatabases` | `listDatabases` (cluster level) |

3. Grant `readWrite` on that database, or switch to `readWriteAnyDatabase`. Grant `dropDatabase` through `dbAdmin` or a custom role only for `dropDatabase` errors, which occur when a namespace is removed.

An error on `System` for `listIndexes` means the credential lacks a grant on the kernel's own read model database. Grant `readWrite` on `System`, `System+es` and `System+es+Default`, with your prefix if you use one.
