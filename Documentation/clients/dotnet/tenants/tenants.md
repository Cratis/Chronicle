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
using Aksio.Cratis.Tenants;

[Route("/api/my-controller")]
public class MyController : Controller
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
using Aksio.Cratis.Execution;
using Aksio.Cratis.Tenants;

[Route("/api/my-controller")]
public class MyController : Controller
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
