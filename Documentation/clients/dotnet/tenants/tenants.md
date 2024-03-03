# Tenants

Cratis is built from the ground up to be multi tenant capable.
With its capability it completely segregates every tenant and its data, but enables the scenario of having
one version of your software deployed and work across all the tenants configured. This also applies
to the Cratis Kernel, which is in fact the source of configuration for all the tenants supported.

An important design decision that has been made is that application developers shouldn't need to think
about tenancy and making this completely transparent. The code can be written as if it is running in a
single tenant environment.

However, there needs to be something that governs which tenant we are currently working on.
Setting the current tenant is accomplished by working with the [execution context](../../../fundamentals/execution-context.md).

## All tenants

The Cratis Kernel owns what tenants are configured. To get this information you have to use `ITenants`.
Below shows a simple ASP.NET controller that iterates all tenants and can then use the tenant identifier:

```csharp
using Cratis.Tenants;

[Route("/api/my-controller")]
public class MyController : ControllerBase
{
    readonly ITenants _tenants;

    public MyController(ITenants tenants)
    {
        _tenants = tenants;
    }

    [HttpPost]
    public async Task PerformSomethingBasedOnTenant()
    {
        foreach (var tenant in await _tenants.All())
        {
            // Do something based on the tenant.
        }
    }
}
```

Typically your code would probably want to delegate work to be done in the context of a given tenant,
this can be accomplished by working with the execution context:

```csharp
using Cratis.Execution;
using Cratis.Tenants;

[Route("/api/my-controller")]
public class MyController : ControllerBase
{
    readonly ITenants _tenants;
    readonly IExecutionContextManager _executionContextManager;

    public MyController(
        ITenants tenants,
        IExecutionContextManager executionContextManager)
    {
        _tenants = tenants;
        _executionContextManager = executionContextManager;
    }

    [HttpPost]
    public async Task PerformSomethingInContextOfEveryTenant()
    {
        foreach (var tenant in await _tenants.All())
        {
            using var scope = _executionContextManager.ForTenant(tenant);
            // Do something in the context of the tenant.
        }
    }
}
```

## Configuration values

For each tenant, you can associate global configuration values. These are stored as simple key / value pairs for every tenant
within the Kernel. The key / value pairs are global across all microservices.

You can retrieve values by using the `ITenantConfiguration` interface and the `GetAllFor()` method to get all the
key / value pairs for a specific tenant:

```csharp
using Cratis.Execution;
using Cratis.Tenants;

[Route("/api/my-controller")]
public class MyController : ControllerBase
{
    readonly ITenants _tenants;
    readonly ITenantConfiguration _tenantConfiguration,
    readonly IExecutionContextManager _executionContextManager;

    public MyController(
        ITenants tenants,
        ITenantConfiguration tenantConfiguration,
        IExecutionContextManager executionContextManager)
    {
        _tenants = tenants;
        _tenantConfiguration = tenantConfiguration;
        _executionContextManager = executionContextManager;
    }

    [HttpPost]
    public async Task PerformSomethingInContextOfEveryTenant()
    {
        foreach (var tenant in await _tenants.All())
        {
            using var scope = _executionContextManager.ForTenant(tenant);
            var configuration = _tenantConfiguration.GetAllFor(scope.TenantId);
            // Do something in the context of the tenant.
        }
    }
}
```

The configuration is encapsulated in a type called `ConfigurationForTenant` which implements a `IDictionary<string, string>`.
This allows you to then access the values directly.

To set a value you can use the Kernel API with the route `/api/configuration/tenants/{tenantId}`.
Below is a sample using **curl** to set a key/value pair running locally:

```shell
curl -X POST http://localhost:8080/api/configuration/tenants/3352d47d-c154-4457-b3fb-8a2efb725113 \
     -H 'Content-Type: application/json' \
     -d '{"key":"Hello","value":"world"}'
```

For local development and working with a team it can be cumbersome to have startup scripts that needs to run to set
default configuration values. The `cratis.json` file supports holding these key / value pairs as well, which means you
can check in your configuration for development purposes.

In the tenant object you can add a `configuration` object holding key / value pairs:

```json
{
    "tenants": {
        "3352d47d-c154-4457-b3fb-8a2efb725113": {
            "name": "development",
            "configuration": {
                "something": "43"
            }
        }
    }
}
```

> Note: Any key/value pairs in the `cratis.json` takes precedence over anything configured in the Kernel using the API.
> At startup the Kernel will take values in `cratis.json` and import these into the Kernel.
