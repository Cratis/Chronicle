# Running Integration Tests

Integration tests run from the `Integration/Client` project and can execute in either in-process or out-of-process mode. They require Docker to be running on your machine and a short manual wait between consecutive runs so Docker has time to tear down and release the fixed MongoDB port.

When you provide no runtime arguments, the suite defaults to:

- mode: `outofprocess`
- database: `mongodb`

## Prerequisites

- Docker running locally
- .NET SDK (version matches `global.json`)
- Built binaries — run `dotnet build` from the repository root before running tests for the first time, or after any source change

## Test Suites

| Suite | Project | Description |
| ----- | ------- | ----------- |
| Client | `Integration/Client` | Runs the shared .NET integration specifications in either `inprocess` or `outofprocess` mode, with runtime storage selected through command-line arguments. |
| API (out-of-process) | `Integration/Api` | Runs a full Chronicle server via Docker and tests the HTTP/gRPC client against it. |

## Running the Tests

### From the command line

Run all tests in a suite with the convenience scripts at the root of each project:

```bash
# Release build (CI default)
cd Integration/Client
./run.sh

# Debug build (faster iteration locally)
./run-debug.sh
```

Or invoke `dotnet test` directly from the repository root:

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

## Important: Wait Between Consecutive Runs

The MongoDB container binds to a fixed host port (`27018`). After a test run ends, Docker's Ryuk reaper removes the container, but the port is not immediately available. If you start a second run before the port is released, the Docker startup will hang until it times out.

**Always wait a few seconds between runs** when re-running tests manually. In CI, each run starts a fresh agent so this is not a concern.

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
