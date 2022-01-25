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
