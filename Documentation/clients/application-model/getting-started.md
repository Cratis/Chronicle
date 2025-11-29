# Getting Started with Application Model

The Application Model client provides higher-level abstractions for building applications with Chronicle, offering automatic dependency injection, simplified command handling, and seamless access to aggregate roots and read models.

## Prerequisites

- .NET 8 or later
- ASP.NET Core application
- Access to a Chronicle event store (local or remote)

## Installation

Add the Application Model NuGet package to your project:

```shell
dotnet add package Cratis.Chronicle.Applications
```

> **Note**: The `Cratis.Chronicle.Applications` has a dependency to `Cratis.Applications`that holds the rest of the application model.
> In addition, this builds on top of `Cratis.Chronicle.AspNetCore`, which assumes your project is an ASP.NET Core project.

## Configuration

### Basic Setup

Configure Chronicle with Application Model support in your `Program.cs`:

```csharp
using Cratis.Chronicle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add ApplicationModel and then configure Chronicle with Application Model support
builder
    .AddCratisApplicationModel()
    .AddCratisChronicle(
        options => options.EventStore = "YourEventStoreName",
        configure => configure.WithApplicationModel());

var app = builder.Build();

// Use both the ApplicationModel and Chronicle
app.UseCratisApplicationModel();
app.UseCratisChronicle();

app.Run();
```

## Verification

To verify that the Application Model is properly configured, you can create a simple command to test the setup:

### Create a Simple Event

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record UserRegistered(string Email, string Name);
```

### Create a Command

```csharp
// Commands/RegisterUserCommand.cs
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record RegisterUser(
    [Key] EventSourceId UserId,
    string Email,
    string Name)
{
    public UserRegistered Handle() =>
        new UserRegistered(Email, Name);
}
```

### Test the Setup

Start your application and make a POST request to test the configuration:

```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "email": "user@example.com",
    "name": "John Doe"
  }'
```

## What's Included

When you configure Chronicle with `.WithApplicationModel()`, the following services are automatically registered:

### Aggregate Root Services

- Automatic discovery and registration of aggregate root types
- Event source ID resolution from command context
- Automatic dependency injection in command handlers

### Read Model Services

- Automatic discovery of read models from projection definitions
- Seamless access to current read model state in commands
- Event source ID-based read model resolution

### Command Integration

- Integration with Cratis Applications command handling
- Automatic event source ID extraction from commands
- Support for multiple command response patterns

## Next Steps

Now that you have Application Model configured, you can explore the key features:

### Learn About Event Metadata

Event metadata is fundamental to Chronicle's operation. Learn how Event Source IDs, Event Source Types, Event Stream Types, and Event Stream IDs are used to organize and categorize events:

[Read more about Event Metadata →](event-metadata/index.md)

### Working with Aggregate Roots

Discover how to leverage aggregate roots in your command handlers:

[Read more about Aggregate Roots →](aggregate-roots.md)

### Accessing Read Models

Learn how to access projected read models within your commands:

[Read more about Read Models →](read-models.md)

### Advanced Command Patterns

Explore sophisticated command handling patterns and best practices:

[Read more about Commands →](commands.md)

## Common Issues

### Missing Event Source ID

If you encounter `UnableToResolveReadModelFromCommandContext` exceptions, ensure your commands provide an event source ID through:

- A property of type `EventSourceId`
- A property marked with `[Key]` attribute
- Implementing `ICanProvideEventSourceId`

### Service Registration Errors

If dependency injection fails for aggregate roots or read models, verify that:

- Your aggregate roots inherit from `AggregateRoot`
- Your projections implement `IProjectionFor<T>`
- `.WithApplicationModel()` is called in your Chronicle configuration

### Connection Issues

If you can't connect to Chronicle, check that:

- Chronicle server is running (if using external server)
- Connection configuration matches your setup
- Firewall rules allow the connection
