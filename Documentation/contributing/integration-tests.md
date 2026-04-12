# Running Integration Tests

Integration tests spin up a real MongoDB replica set inside Docker and run the full Chronicle stack in-process or out-of-process. They require Docker to be running on your machine and a short manual wait between consecutive runs so Docker has time to tear down and release the fixed MongoDB port.

## Prerequisites

- Docker running locally
- .NET SDK (version matches `global.json`)
- Built binaries — run `dotnet build` from the repository root before running tests for the first time, or after any source change

## Test Suites

| Suite | Project | Description |
| ----- | ------- | ----------- |
| In-Process | `Integration/DotNET.InProcess` | Runs Chronicle kernel and client inside the same process using the Orleans in-process hosting model. |
| API (out-of-process) | `Integration/Api` | Runs a full Chronicle server via Docker and tests the HTTP/gRPC client against it. |

## Running the Tests

### From the command line

Run all tests in a suite with the convenience scripts at the root of each project:

```bash
# Release build (CI default)
cd Integration/DotNET.InProcess
./run.sh

# Debug build (faster iteration locally)
./run-debug.sh
```

Or invoke `dotnet test` directly from the repository root:

```bash
dotnet test Integration/DotNET.InProcess/DotNET.InProcess.csproj \
    --logger "console;verbosity=normal" \
    --configuration Release \
    --framework net10.0
```

### Running a single test

Use `--filter` with the fully qualified type name or a substring of it:

```bash
dotnet test Integration/DotNET.InProcess/DotNET.InProcess.csproj \
    --filter "FullyQualifiedName~for_EventSequence.when_appending.an_event" \
    --no-build
```

## Important: Wait Between Consecutive Runs

The MongoDB container binds to a fixed host port (`27018`). After a test run ends, Docker's Ryuk reaper removes the container, but the port is not immediately available. If you start a second run before the port is released, the Docker startup will hang until it times out.

**Always wait a few seconds between runs** when re-running tests manually. In CI, each run starts a fresh agent so this is not a concern.

## Test Backups

Each test collection automatically takes a MongoDB backup at the end of a run when the backup feature is enabled. Backups are useful for inspecting the state left by a failing test.

### Enabling backups

Set the `CHRONICLE_BACKUP_ENABLED` environment variable to `true` before running tests:

```bash
CHRONICLE_BACKUP_ENABLED=true dotnet test Integration/DotNET.InProcess/DotNET.InProcess.csproj
```

### Where backups are stored

Backups are written to a `backups/` directory that sits alongside the compiled test binary. For a Debug build targeting `net10.0`, that path is:

```
Integration/DotNET.InProcess/bin/Debug/net10.0/backups/
```

The directory is created automatically during fixture initialization — you do not need to create it yourself.

### Backup file naming

Each backup file is a gzip-compressed MongoDB archive dump with the following naming pattern:

```
{prefix}-yyyyMMdd-HHmmss.tgz
```

The prefix corresponds to the xUnit collection name registered for the test suite. When no prefix is set the timestamp is used alone:

```
20260412-143025.tgz
```

### Inspecting a backup

Restore a backup to a local `mongod` instance to inspect the data:

```bash
mongorestore --gzip --archive=backups/20260412-143025.tgz
```
