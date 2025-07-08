# Chronicle SQL Storage

This project provides SQL database storage support for Chronicle projections and reducers. It supports both SQL Server and PostgreSQL databases through Entity Framework Core.

## Features

- **Multi-Provider Support**: Works with SQL Server and PostgreSQL
- **Dynamic Schema Generation**: Automatically creates tables based on projection schemas  
- **Schema Evolution**: Handles adding new columns as schemas change
- **Tenancy Support**: Configurable schema-based or table-based multi-tenancy
- **Entity Framework Integration**: Uses EF Core for database operations and migrations

## Quick Start

### SQL Server Configuration

```csharp
services.AddSqlServerStorage(
    connectionString: "Server=localhost;Database=Chronicle;Integrated Security=true;",
    options => 
    {
        options.Schema = "projections";
        options.UseSchemaForNamespacing = true;
    });
```

### PostgreSQL Configuration

```csharp
services.AddPostgreSqlStorage(
    connectionString: "Host=localhost;Database=chronicle;Username=user;Password=pass",
    options => 
    {
        options.Schema = "public";
        options.UseSchemaForNamespacing = false;
    });
```

### Generic Configuration

```csharp
services.AddSqlStorage(options =>
{
    options.ProviderType = SqlProviderType.SqlServer;
    options.ConnectionString = "your-connection-string";
    options.Schema = "dbo";
    options.UseSchemaForNamespacing = true;
    options.AutoCreateSchema = true;
});
```

## Configuration Options

### SqlStorageOptions

- **ProviderType**: `SqlServer` or `PostgreSQL`
- **ConnectionString**: Database connection string
- **Schema**: Default schema name (default: "dbo")
- **UseSchemaForNamespacing**: Use schemas for tenant separation (default: true)  
- **AutoCreateSchema**: Automatically create schemas/tables (default: true)

## Architecture

### Core Components

- **Sink**: Implements the `ISink` interface for SQL databases
- **SinkFactory**: Creates sink instances with proper configuration
- **SqlSchemaGenerator**: Generates DDL statements from JSON schemas
- **DynamicModelManager**: Handles table creation and schema evolution
- **ChangesetConverter**: Converts Chronicle changesets to SQL operations

### Table Structure

Each projection is stored in a table with the following base columns:

- `Id` (NVARCHAR/VARCHAR): Primary key
- `EventSequenceNumber` (BIGINT): Last processed event sequence number
- `LastUpdated` (DATETIME2/TIMESTAMP): Last modification timestamp  
- `Data` (NVARCHAR(MAX)/TEXT): JSON representation of the projection data

Additional columns are generated based on the projection's JSON schema for primitive types.

## Tenancy Strategies

### Schema-based Tenancy (Recommended)
Each namespace gets its own database schema:
```csharp
options.UseSchemaForNamespacing = true;
```

### Table-based Tenancy  
All tenants share the same schema but have separate tables:
```csharp
options.UseSchemaForNamespacing = false;
```

## Schema Evolution

The SQL storage automatically handles schema changes:

1. **New Tables**: Created automatically when first accessed
2. **New Columns**: Added when new properties are detected in schemas
3. **Type Changes**: Handled gracefully by storing complex types as JSON

## Performance Considerations

- **Indexing**: Add indexes on frequently queried columns manually
- **Partitioning**: Consider table partitioning for large datasets
- **Connection Pooling**: Configure connection pooling in your connection string
- **Batch Operations**: Changesets are applied as individual operations

## Limitations

- Complex nested objects are stored as JSON in the `Data` column
- Array/collection properties are stored as JSON 
- Schema downgrade (removing columns) requires manual intervention
- No automatic index creation

## Examples

### Basic Projection Storage

```csharp
// Chronicle will automatically create a table for this projection
public class UserProjection
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

Generated table structure:
```sql
CREATE TABLE projections.projection_user (
    Id NVARCHAR(255) PRIMARY KEY,
    EventSequenceNumber BIGINT NOT NULL,
    LastUpdated DATETIME2 DEFAULT GETUTCDATE(),
    Name NVARCHAR(450),
    Email NVARCHAR(450), 
    CreatedAt DATETIME2,
    IsActive BIT,
    Data NVARCHAR(MAX)
)
```

## Testing

The project includes unit tests that verify:
- Sink factory creation and configuration
- SQL schema generation for different providers
- Table creation logic
- Basic CRUD operations

Run tests with:
```bash
dotnet test Source/Kernel/Storage.SQL.Specs
```

## Contributing

When contributing to this project:
1. Follow the existing code style and patterns
2. Add tests for new functionality
3. Update documentation for new features
4. Consider performance implications of changes