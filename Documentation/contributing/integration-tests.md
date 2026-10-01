---
title: "Running Integration Tests"
description: "Run Chronicle's client integration tests in-process or out-of-process against each supported database."
---

Integration tests run from the `Integration/Client` project and can execute in either in-process or out-of-process mode. They require Docker to be running on your machine.

When you provide no runtime arguments, the suite defaults to:

- mode: `outofprocess`
- database: `mongodb`

## When CI runs integration

Pull-request pushes affecting the [build paths](https://github.com/Cratis/Chronicle/blob/main/.github/build-affecting-paths.txt) run the Release build, specs, and a local Docker image build from Release binaries without pushing it, **not** the Docker integration suites. Run the relevant integration namespace locally before requesting CI integration. The [hot-core gate](https://github.com/Cratis/Chronicle/blob/main/.github/hot-core-paths.txt) reports which hot-core paths a PR touches; its ordinary PR check does not run the matrix.

Apply the `run-integration` label to a pull request to start the separate Requested Integration workflow, even for a documentation-only PR. It builds the commit and runs specs, API and MongoDB integration, and one client integration matrix: MongoDB and SQLite normally, every backend when the PR touches the hot core. The separately named requested hot-core gate requires that full matrix to pass. Remove and re-add the label to request another run after changes; pushing new commits does not rerun integration automatically. Other labels do not start integration or replace the ordinary PR check results.

The existing nightly schedule on `main` and manual `workflow_dispatch` run the full client backend matrix. Main-branch pushes do not run integration; nightly coverage handles main. A fork PR cannot push the integration image with its read-only token; labeling it fails the requested gate with an "unavailable" notice instead of running integration, so a fork PR can never satisfy that gate. Ask a maintainer to dispatch the workflow on a trusted branch containing those changes.

For a labeled event, the image-push guard checks `github.actor`—the account that applied the label, not the PR author. A maintainer-labeled Dependabot PR therefore attempts the image push; if GitHub supplies a read-only token, the requested run fails rather than silently skipping the client matrix.

## Prerequisites

- Docker running locally
- .NET SDK (version matches `global.json`)
- Built binaries — run `dotnet build` from the repository root before running tests for the first time, or after any source change

## Test Suites

| Suite | Project | Description |
| ----- | ------- | ----------- |
| Client | `Integration/Client` | Runs the shared .NET integration specifications in either `inprocess` or `outofprocess` mode, with runtime storage selected through command-line arguments. |
| API (out-of-process) | `Integration/Api` | Runs a full Chronicle server via Docker and tests the HTTP/gRPC client against it. |

## Checking packed test-package startup

Before publishing locally packed packages, check startup from outside the repository:

```bash
bash .github/scripts/verify-consumer-smoke.sh "$CHRONICLE_VERSION" "$LOCAL_PACKAGE_FEED"
```

Set `CHRONICLE_VERSION` to the candidate version and `LOCAL_PACKAGE_FEED` to the directory containing its NuGet packages. The check requires the .NET SDK and NuGet access, but not Docker. It starts two separate applications: one using Chronicle.Testing with Arc.Chronicle.Testing, and one using Chronicle.XUnit.Integration without either Testing package. Both must complete `AddCratisArcCore()` type discovery.

Each test package carries its own embedded kernel runtime assemblies, including Compatibility. Do not add a separate Compatibility package to the consumer. Testing one package alongside the other can hide a missing runtime assembly, so keep the two consumer graphs separate. This startup check does not replace the kernel/storage integration suites below.

## Running the Tests

### From the command line

Run the suite with `dotnet test` from the repository root. `--configuration Release` matches CI; use `Debug` for faster local iteration:

```bash
dotnet test Integration/Client/Client.csproj \
    --logger "console;verbosity=normal" \
    --configuration Release \
    --framework net10.0 \
    -- inprocess mongodb
```

### Runtime and database configurations (CLI)

The first positional argument after `--` is the mode, and the second is the database:

```bash
dotnet test Integration/Client/Client.csproj -- outofprocess mongodb
dotnet test Integration/Client/Client.csproj -- outofprocess postgresql
dotnet test Integration/Client/Client.csproj -- outofprocess mssql
dotnet test Integration/Client/Client.csproj -- outofprocess sqlite
dotnet test Integration/Client/Client.csproj -- inprocess mongodb
```

Named arguments are also supported:

```bash
dotnet test Integration/Client/Client.csproj -- --mode=outofprocess --database=postgresql
dotnet test Integration/Client/Client.csproj -- --mode=inprocess --db=mongodb
```

Alternatively, configure via environment variables or `--environment` flags:

```bash
dotnet test Integration/Client/Client.csproj --environment CHRONICLE_RUNTIME_MODE=inprocess
CHRONICLE_RUNTIME_MODE=inprocess dotnet test Integration/Client/Client.csproj
```

For PostgreSQL and MsSql, set connection details before running tests.
The values below are examples only — replace credentials and hosts with values from your local environment:

```bash
export CHRONICLE_POSTGRESQL_CONNECTION_DETAILS="Host=localhost;Port=5432;Database=chronicle;Username=postgres;Password=postgres"
export CHRONICLE_MSSQL_CONNECTION_DETAILS="Server=localhost,1433;Database=chronicle;User Id=sa;Password=Your_password123;TrustServerCertificate=true"
```

For SQLite, you can optionally set:

```bash
export CHRONICLE_SQLITE_CONNECTION_DETAILS="Data Source=/tmp/chronicle.db"
```

### Running against an external MongoDB

> **Data-loss warning:** Never run out-of-process tests against a server containing valuable data with an older Chronicle kernel image. Images that ignore `DatabaseNamePrefix` can delete unrelated databases during reset. Build the current kernel image and select it with `CRATIS_CHRONICLE_LOCAL_IMAGE`. The client fixture refuses reset unless its prefixed cluster database exists and no new unprefixed Chronicle databases appeared after startup; this check does not make old images safe for shared servers.

Set `CHRONICLE_MONGODB_CONNECTION_DETAILS` to a MongoDB connection string. The client, kernel, API, and MongoDB integration fixtures and `Storage.MongoDB.Specs` use it instead of starting their own MongoDB. The client and API out-of-process modes still require Docker for the Chronicle kernel.

```bash
export CHRONICLE_MONGODB_CONNECTION_DETAILS="mongodb://mongo.example.test:27017/?replicaSet=myReplicaSet"
CHRONICLE_RUNTIME_MODE=inprocess CHRONICLE_STORAGE_PROVIDER=mongodb \
    dotnet test Integration/Client/Client.csproj --configuration Release

dotnet test Integration/MongoDB/MongoDB.csproj --configuration Release

dotnet test Source/Kernel/Storage.MongoDB.Specs/Storage.MongoDB.Specs.csproj --configuration Release
```

Use the service's connection string unchanged, including authentication, TLS, replica-set, or SRV options. For out-of-process tests, its address must be reachable both from your host and from the Chronicle container; `localhost` inside that container is not your host. Build a kernel image containing the current changes before running out-of-process tests.

Each fixture generates a unique database-name prefix and configures the kernel and its database readers to use it. Cleanup and kernel resets are restricted to that prefix; unrelated databases are never dropped. Storage specs also create unique, run-prefixed databases and remove only the databases they create. Container restarts, replica-set initiation, and `mongodump` backups are skipped for external MongoDB, even with `CHRONICLE_BACKUP_ENABLED=true`.

The external service must support the MongoDB features used by the selected suite, including multi-document transactions and change streams for event-sequence and observation specs. Grant permissions to create and drop databases, create indexes, read/write collections, and enumerate database names. Providing a connection string does not imply compatibility with a managed service; a failing spec can reveal an unsupported feature.

Unset `CHRONICLE_MONGODB_CONNECTION_DETAILS` to return to the default container-backed fixtures. `CHRONICLE_MONGODB_IMAGE` and `CHRONICLE_SPECS_MONGODB_IMAGE` apply only when the fixtures start MongoDB themselves.

### Running a single test

Use `--filter` with the fully qualified type name or a substring of it:

```bash
dotnet test Integration/Client/Client.csproj \
    --filter "FullyQualifiedName~for_EventSequence.when_appending.an_event" \
    --no-build \
    -- outofprocess mongodb
```

### From VS Code

You can run the same configurations from VS Code by creating a dedicated test launch configuration in `.vscode/launch.json`:

```json
{
  "name": ".NET Test (Client integration - outofprocess mongodb)",
  "type": "coreclr",
  "request": "launch",
  "program": "dotnet",
  "args": [
    "test",
    "${workspaceFolder}/Integration/Client/Client.csproj",
    "--framework",
    "net10.0",
    "--configuration",
    "Debug",
    "--",
    "outofprocess",
    "mongodb"
  ],
  "cwd": "${workspaceFolder}",
  "console": "integratedTerminal"
}
```

For in-process mode, change the last two arguments to `"inprocess"` and `"mongodb"`.

For database changes in out-of-process mode, set the second argument to `"postgresql"`, `"mssql"`, or `"sqlite"`.

## Consecutive runs

The `Integration/Client` fixtures publish every container port on a random host port, so consecutive runs — or two runs at once — of that suite do not compete for a port. Docker's Ryuk reaper removes the containers when a run ends.

The `Integration/Api` suite, and the reusable out-of-process fixture in `Cratis.Chronicle.XUnit.Integration`, bind fixed host ports (`27018` and `35001`). Two of those runs at once collide, and a run started immediately after another can wait for Docker to release the ports.

## Test Backups

Each test collection automatically takes a MongoDB backup at the end of a run when the backup feature is enabled. Backups are useful for inspecting the state left by a failing test.

### Enabling backups

Set the `CHRONICLE_BACKUP_ENABLED` environment variable to `true` before running tests:

```bash
CHRONICLE_BACKUP_ENABLED=true dotnet test Integration/Client/Client.csproj \
    --environment CHRONICLE_RUNTIME_MODE=inprocess \
    --environment CHRONICLE_STORAGE_PROVIDER=mongodb
```

### Where backups are stored

Backups are written to a `backups/` directory that sits alongside the compiled test binary. For a Debug build targeting `net10.0`, that path is:

```bash
Integration/Client/bin/Debug/net10.0/backups/
```

The directory is created automatically during fixture initialization — you do not need to create it yourself.

### Backup file naming

Each backup file is a gzip-compressed MongoDB archive dump with the following naming pattern:

```bash
{prefix}-yyyyMMdd-HHmmss.tgz
```

The prefix corresponds to the xUnit collection name registered for the test suite. When no prefix is set the timestamp is used alone:

```bash
20260412-143025.tgz
```

### Inspecting a backup

Restore a backup to a local `mongod` instance to inspect the data:

```bash
mongorestore --gzip --archive=backups/20260412-143025.tgz
```
