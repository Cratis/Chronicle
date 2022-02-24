# MongoDB

MongoDB is a managed resource. This means that its configuration is managed by Cratis Kernel.
In addition, MongoDB is also considered a
[multi-tenant](../tenancy.md). resource which means that there should be one configuration / database
per [tenant](../tenancy.md).

For you as a developer, it has been made very simple by letting you work with the MongoDB C# Driver API only through
a pre-configured setup for the [IoC Container](./ioc.md).

The configuration of storage is done through the Cratis Kernel, read more [here](../../kernel/storage.md).

Usage:

Lets say we have a document that is defined as the following:

```csharp
public record Employee(Guid Id, string FirstName, string LastName);
```

```csharp
[Route("/api/employees")]
public class EmployeesController : Controller
{
    readonly IMongoDatabase _collection;

    public EmployeesController(IMongoCollection<Employee> collection) => _collection = collection;

    [HttpGet]
    public IEnumerable<Employee> GetAllEmployees() = () => _collection.Find(_ => true).ToList();
}
```

> Note: The name of the collection will follow the convention of pluralizing the name and then **camelCasing** it.

```csharp
[Route("/api/employees")]
public class EmployeesController : Controller
{
    readonly IMongoDatabase _database;

    public EmployeesController(IMongoDatabase database) => _database = database;

    [HttpGet]
    public IEnumerable<Employee> GetAllEmployees()
    {
        var collection = _database.GetCollection<Employee>();
        return collection.Find(_ => true).ToList();
    }
}
```

The `GetCollection<Employee>()` call is an extension method provided by the application model that automatically by convention
pluralizes the name of the collection and also makes it **camelCase**.

If you need to control the name of the collection, you can use one of the overrides for this:

```csharp
[Route("/api/employees")]
public class EmployeesController : Controller
{
    readonly IMongoDatabase _database;

    public EmployeesController(IMongoDatabase database) => _database = database;

    [HttpGet]
    public IEnumerable<Employee> GetAllEmployees()
    {
        var collection = _database.GetCollection<Employee>("MyEmployees");
        return collection.FindAsync(_ => true).ToList();
    }
}
```

> Important: It is very important that you don't create a singleton and keep the instance around, as you will be handed the
> correct instance for the tenant in the current context.

For more advanced topics on queries and observable queries, read more [here](./cqrs/queries.md).

## Conventions

Out of the box the system applies conventions related to naming of collections and properties on models.

### Properties

For properties the following convention packs are default turned on:

| Type | Description |
| ---- | ----------- |
| CamelCase | Turns properties into camelCase from a typical PascalCase casing in C# |
| IgnoreExtraElements | Ignores extra elements encountered during deserialization |

Turning off the property conventions you have two options, create a type that implements
the interface `ICanFilterMongoDBConventionPacksForType` or adorn the type(s) with
`[IgnoreConventions]` attribute.

```csharp
public class MyCustomFilter : ICanFilterMongoDBConventionPacksForType
{
    public bool ShouldInclude(string conventionPackName, IConventionPack conventionPack, Type type) => type == typeof(Employee) && ConventionPack == ConventionPacks.CamelCase;
}
```

Using the attribute approach:

```csharp
[IgnoreConventions(ConventionPacks.CamelCase)]
public record Employee(Guid Id, string FirstName, string LastName);
```

> Note: The attribute takes a params of conventions, you can simply just add another parameter with any convention pack to ignore.
> If you want to ignore all conventions, you can simply use the attribute without any parameters: `[IgnoreConventions]``

### Collection names

By default all collection names are pluralized assuming that the model type is non-pluralized. On top of this it makes the collection name
camelCase as well.

You can either control this behavior by adding a [class map](https://mongodb.github.io/mongo-csharp-driver/2.10/reference/bson/mapping/).
You do not have to explicitly register it as the system will automatically discover any implementations of the `IBsonClassMapFor<>` interface:

```csharp
public class EmployeeClassMap : IBsonClassMapFor<Employee>
{
    public void Configure(BsonClassMap<T> classMap)
    {
        classMap.AutoMap();
        classMap.SetDiscriminator("AllEmployees");
        classMap.MapMember(c => c.FirstName);
        classMap.MapMember(c => c.LastName);
    }
}
```

To set the model name on the model itself, you can leverage the `[ModelName]` attribute. This is also honored by projection by the Cratis Kernel.

```csharp
[ModelName("AllEmployees"))]
public record Employee(Guid Id, string FirstName, string LastName);
```
