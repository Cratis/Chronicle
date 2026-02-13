# Camel Casing

Chronicle can apply a camel case naming policy when building projection definitions and persisting read models. This keeps property names consistent with common JSON naming conventions.

## Overview

By default, Chronicle uses property names as they appear in C# (PascalCase). Enabling camel case will:

- Govern how projection definitions are built
- Determine the property names used when projections update read models
- Keep naming consistent across projection and read model data

## Using a Regular Client

Configure the client with camel case naming policy by passing options:

```csharp
var options = new ChronicleOptions();
options.WithCamelCaseNamingPolicy();

var client = new ChronicleClient(options);
```

### Configuration with Additional Options

You can combine camel case naming policy with other Chronicle configuration options:

```csharp
var options = new ChronicleOptions
{
    EventStore = "MyEventStore"
};
options.WithCamelCaseNamingPolicy();

var client = new ChronicleClient(options);
```

## Using ASP.NET Core

When building ASP.NET Core applications, use the dependency injection extensions to configure Chronicle with camel case naming policy.

### Basic Configuration

In your `Program.cs` file, configure Chronicle with camel case naming policy:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChronicle(options =>
{
    options.WithCamelCaseNamingPolicy();
});

var app = builder.Build();
```

### Dependency Injection Configuration

You can combine camel case naming policy with other Chronicle configuration options:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChronicle(options =>
{
    options.EventStore = "MyEventStore";
    options.WithCamelCaseNamingPolicy();
});

var app = builder.Build();
```

## Impact on Projections

The naming policy configuration affects how Chronicle handles projections:

### Projection Definition Building

When you configure camel case naming policy, Chronicle uses this policy when building projection definitions. This means property mappings within projections will use camel case property names.

### Read Models

The naming policy affects the read model container name, which affects the name of the collection, table, or file that data is persisted as.

When projections project to read models, the property names used will follow the configured naming policy. For example:

#### Event Definition

```csharp
public record UserRegistered(
    string FirstName,
    string LastName,
    string EmailAddress,
    DateTime RegistrationDate);
```

#### Read Model Definition

```csharp
public class UserReadModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public DateTime RegistrationDate { get; set; }
}
```

#### Projection with Camel Case Naming Policy

With camel case naming policy configured, Chronicle will use camel case property names when building the projection:

```csharp
protected override void DefineModel(IProjectionBuilderFor<UserReadModel> builder) =>
    builder
        .From<UserRegistered>(_ => _
            .Set(m => m.FirstName).To(e => e.FirstName)
            .Set(m => m.LastName).To(e => e.LastName)
            .Set(m => m.EmailAddress).To(e => e.EmailAddress)
            .Set(m => m.RegistrationDate).To(e => e.RegistrationDate));
```

The resulting read model data will have camel case property names: `firstName`, `lastName`, `emailAddress`, `registrationDate`.

## Important Notes

- The naming policy affects how Chronicle builds projection definitions internally.
- Property names in read models will follow the configured naming policy when data is persisted.
- This configuration is specific to Chronicle operations and does not affect general ASP.NET Core JSON serialization.
- All projections in your application will use the same naming policy once configured.

## Troubleshooting

### Projection Property Names Not Converting

If projection property names are not being converted to camel case:

1. Verify that `WithCamelCaseNamingPolicy()` is called during Chronicle configuration.
2. Ensure the configuration is applied before Chronicle services are initialized.
3. Check that all projections are using the same Chronicle client instance.

### Inconsistent Property Naming

If you see inconsistent property naming in your read models:

1. Verify that Chronicle is configured with the camel case naming policy.
2. Check if any custom property mappings are overriding the global naming policy.
3. Ensure all projection definitions are rebuilt after changing the naming policy.

