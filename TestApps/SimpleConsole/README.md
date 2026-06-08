# SimpleConsole Test App

An interactive test application demonstrating the Cratis Chronicle client library. Manage employee records with event sourcing, including promotions, address changes, email management, and read model queries.

## Prerequisites

- .NET 10 or later
- Chronicle Kernel running on `localhost:35000`
- The appropriate database backend for your chosen storage type (MongoDB, PostgreSQL, SQL Server, or SQLite)

## Running the App

### MongoDB (default)

```bash
dotnet run
```

Or explicitly:

```bash
dotnet run mongodb
```

Requires MongoDB running on `localhost:27017`.

### PostgreSQL

```bash
dotnet run postgresql
```

Requires PostgreSQL running on `localhost:5432` with:
- Database: `chronicle`
- Username: `chronicle`
- Password: `chronicle`

### Microsoft SQL Server

```bash
dotnet run mssql
```

Requires SQL Server running on `localhost:1433` with:
- Database: `chronicle`
- SA login credentials configured

### SQLite

```bash
dotnet run sqlite
```

SQLite requires no external setup — the database file is created automatically.

## Database Configuration at Kernel Level

The `DefaultSinkTypeId` is automatically set based on the database argument:
- **MongoDB** → uses MongoDB sink
- **PostgreSQL, MSSQL, SQLite** → use SQL sink

The Kernel must be running with the matching storage backend. For example, if you run `dotnet run postgresql`, the Kernel must be configured with PostgreSQL storage.

## Interactive Commands

Once running, interact with the app using keyboard commands:

| Key | Action |
|-----|--------|
| **1-3** | Select an employee (1=Alice, 2=Bob, 3=Charlie) |
| **P** | Promote selected employee to a random title |
| **A** | Move employee to a random address |
| **E** | Set email for selected employee |
| **U** | Attempt to take another employee's email (triggers constraint violation) |
| **R** | Display read model for selected employee |
| **T** | Transactional update (promotes two employees in one transaction) |
| **I** | Switch to next user (cycles through Alice, Bob, System) |
| **H** | Show help menu |
| **Q** | Quit the application |

## What It Demonstrates

### Event Sourcing
- Appending events to the event log
- Multiple event types: `EmployeePromoted`, `EmployeeMoved`, `EmployeeEmailSet`
- Event migrations for schema evolution

### Constraints
- Unique email constraint prevents duplicate email addresses
- Constraint violations are caught and reported

### Read Models
- Real-time projection of events into `EmployeeState` read model
- Query read models by event source ID
- Display projected state: title, email, address

### Transactional Updates
- Group multiple events from different entities
- Commit atomically using `UnitOfWorkManager`

### Multi-User Context
- Switch between different users (Alice Smith, Bob Jones, System)
- Commands executed under different user contexts

### Compliance & PII
- Email addresses are treated as PII
- Encryption/decryption demonstrated through read model queries

## Connecting to Different Chronicle Instances

The default connection is to `localhost:35000`. To connect to a different Kernel instance, edit line 19 in `Program.cs`:

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://chronicle-dev-client:chronicle-dev-secret@<host>:<port>");
```

The connection string format is:
```
chronicle://<client-id>:<client-secret>@<host>:<port>
```

## Troubleshooting

### "Connection refused" or "Connection timeout"
- Ensure the Chronicle Kernel is running on `localhost:35000`
- Verify the Kernel is configured with the correct storage backend matching your database argument

### "Database connection failed"
- Verify the database (MongoDB, PostgreSQL, SQL Server, or SQLite) is running
- Check the connection string matches your database configuration
- For PostgreSQL/MSSQL: ensure the `chronicle` database exists and credentials are correct

### "Constraint violation" errors
- Constraint violations are expected when running the **U** command (attempting to steal an email)
- This demonstrates the constraint system working correctly

## Architecture

The app is structured around several key domain concepts:

```
Employee (event source)
├── EmployeePromoted (event)
├── EmployeeMoved (event)
├── EmployeeEmailSet (event)
└── EmployeeState (read model)
```

Commands are executed asynchronously and flow through:
1. Event appending to the event log
2. Constraint validation
3. Event projection into the `EmployeeState` read model
4. Query results displayed to the user

## Related Files

- `Program.cs` — main application entry point
- `Employee.cs` — domain model
- `EmployeeData.cs` — sample data
- `EmployeeSeeding.cs` — initial data seeding
- `Reactors.cs` — event-driven side effects
- `Constraints.cs` — business rule enforcement
- `MyEvent.cs` — core event types
- `MyEventMigration.cs` — event schema evolution example
