# Execution Context

The execution context holds values that are important to be kept on a per call context level.
Leveraging internally the `AsyncLocal<>` construct to make sure the value is kept consistent
per call context honoring the async context it is in.

Any subsequent calls or child call contexts will inherit the execution context until it is
explicitly changed.

Moving back up the hierarchy the execution context will go out of scope when it is no longer
in the call context it was established.

On the execution context you find the following properties:

| Property | Description |
| -------- | ----------- |
| MicroserviceId | The current microservice identifier |
| TenantId | The identifier of the current tenant |
| CorrelationId | The unique correlation identifier for tracing distributed requests |
| CausationId | The identifier for the causation hierarchy |
| CausedBy | The identifier of the person or system that is the cause for the call |

## Execution Context Middleware

For ASP.NET Core applications, the execution context gets automatically established for every Web request coming in.
All properties are set based on looking at the HTTP request, what action is being performed and the user or system
performing it. The `TenantId` property is retrieved from an HTTP header - `Tenant-ID`. If this header is not present,
it will assume the `Development` tenant.

> Note: Development tenant is a default tenant with a well known identifier (`3352d47d-c154-4457-b3fb-8a2efb725113`).
> When creating a new microservice > using the Cratis templates, it configures this tenant as the default in the configuration.

If you're working on a multi tenant solution and you want to set the tenant identifier, you can use extensions to your
browser, such as [ModHeader](https://modheader.com/). For APIs, you simply add the HTTP header in the tool you're using.

## Getting the execution context

The current execution context can be retrieved by taking a dependency to the `IExecutionContextManager`
in a constructor in your code. On there you'll find a property called `Current`:

```csharp
using Aksio.Cratis.Execution;

[Route("/api/my-controller")]
public class MyController : Controller
{
    readonly IExecutionContextManager _executionContextManager;

    public MyController(IExecutionContextManager executionContextManager)
    {
        _tenants = tenants;
        _executionContextManager = executionContextManager;
    }

    [HttpPost]
    public async Task DoSomething()
    {
        var tenantId = _executionContextManager.Current.TenantId;

        // Do something with the tenantId
    }
}
```

## Setting the execution context

On the `IExecutionContextManager` you find a method called `Establish()` that allows you
to set the current execution context for your call context.

```csharp
using Aksio.Cratis.Execution;

[Route("/api/my-controller")]
public class MyController : Controller
{
    readonly IExecutionContextManager _executionContextManager;

    public MyController(IExecutionContextManager executionContextManager)
    {
        _tenants = tenants;
        _executionContextManager = executionContextManager;
    }

    [HttpPost]
    public async Task DoSomethingForSpecificTenant()
    {
        _executionContextManager.Establish("709b75d4-f794-4680-a767-a7c7d69d98ac")

        // Do something in the context of the tenant
    }
}
```

## Setting the execution context temporarily

When working in a multi tenant environment, there are scenarios were you want to be more deliberate in setting
the execution context and make sure its only temporary.

For this scenario, the `IExecutionContextManager` has a method called `ForTenant()` that returns a disposable
`ExecutionContextScope`.

```csharp
using Aksio.Cratis.Execution;

[Route("/api/my-controller")]
public class MyController : Controller
{
    readonly IExecutionContextManager _executionContextManager;

    public MyController(IExecutionContextManager executionContextManager)
    {
        _tenants = tenants;
        _executionContextManager = executionContextManager;
    }

    [HttpPost]
    public async Task DoSomethingForSpecificTenant()
    {
        using var scope = _executionContextManager.ForTenant("709b75d4-f794-4680-a767-a7c7d69d98ac")
        // Do something in the context of the tenant
    }
}
```
