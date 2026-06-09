# SimpleConsole Test App

An interactive console application demonstrating the Cratis Chronicle .NET client library.
It is the C# counterpart to the TypeScript `Samples/Console` sample and covers the same features.

## What it demonstrates

1. Appends domain events (`EmployeeHired`, `EmployeeEmailSet`, `EmployeePromoted`, `EmployeeAddressSet`, `EmployeeMoved`) to a Chronicle event store
2. Reacts to those events via `HrNotificationReactor` (console notifications)
3. Seeds initial employee state with `EmployeeSeeding` (`ICanSeedEvents`)
4. Enforces two discoverable `IConstraint` artifacts:
   - `UniqueEmployeeHire` — a unique-event-type constraint; an employee can only be hired once
   - `UniqueEmployeeEmail` — a unique-value constraint backed by a Kernel index; rejects duplicate email addresses
5. Reads event log state back through the `EmployeeState` model-bound projection (R key)
6. Demonstrates Unit of Work transactions with `store.EventLog.Transactional` and `UnitOfWorkManager` (T key)
7. Demonstrates declarative projections (`EmployeeListProjection`) and model-bound projections (`EmployeeDetails`)
8. Demonstrates compliance features with `[PII]` on `ConceptAs<T>` types for protecting Personally Identifiable Information (C/V keys)
9. Bootstraps OpenTelemetry tracing and metrics when `OTEL_EXPORTER_OTLP_ENDPOINT` is set

## Keyboard controls

Select an employee with `1`–`3`, then:

| Key | Action |
| --- | --- |
| `P` | Promote the selected employee to a new title |
| `A` | Move the selected employee to a new address |
| `E` | Set the selected employee's own (unique) email address |
| `U` | Attempt to take the next employee's email — rejected by the `UniqueEmployeeEmail` constraint |
| `R` | Read the selected employee's `EmployeeState` read model |
| `T` | Commit a transactional (Unit of Work) batch of events |
| `C` | Register a sample customer with PII-carrying events |
| `V` | View the PII-encrypted customer read model |
| `I` | Cycle the acting user (Alice Smith → Bob Jones → System) |
| `H` or `?` | Show the keyboard menu |
| `Q` | Quit |

## Prerequisites

- .NET 10 or later
- A Chronicle Kernel running on `localhost:35000`
- The appropriate database backend for your chosen storage type (MongoDB, PostgreSQL, SQL Server, or SQLite)

## Running

### MongoDB (default)

```bash
dotnet run
```

### PostgreSQL

```bash
dotnet run postgresql
```

### Microsoft SQL Server

```bash
dotnet run mssql
```

### SQLite

```bash
dotnet run sqlite
```

## OpenTelemetry

Set `OTEL_EXPORTER_OTLP_ENDPOINT` to activate tracing and metrics export:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run
```

Override the service name and version with `SERVICE_NAME` and `SERVICE_VERSION`.

## Connecting to a different Chronicle instance

Edit the connection string on line 24 of `Program.cs`:

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://chronicle-dev-client:chronicle-dev-secret@<host>:<port>");
```

## Project structure

```text
SimpleConsole/
  Program.cs                    # Interactive console entry point
  EmployeeData.cs               # Shared employee data and helpers
  Telemetry.cs                  # OpenTelemetry bootstrap
  Employee.cs                   # Employee events, EmployeeState model-bound projection, EmployeeStateReducer
  Projections.cs                # EmployeeListProjection (declarative) + EmployeeDetails (model-bound)
  Reactors.cs                   # HrNotificationReactor — event-driven side effects
  EmployeeSeeding.cs            # EmployeeSeeding — ICanSeedEvents artifact
  Constraints.cs                # UniqueEmployeeHire + UniqueEmployeeEmail constraint artifacts
  Compliance.cs                 # PII ConceptAs types, Customer events/read model/reducer, demo helpers
```
