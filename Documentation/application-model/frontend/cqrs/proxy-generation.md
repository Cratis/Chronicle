# Proxy Generation

To bridge the gap between the frontend and the backend, there is a tool for generating what we call proxies.
These are representations to be used in the frontend for artifacts in the backend. These are primarily grouped into 2
types; Commands & Queries.

The proxy generator runs as part of your build process leveraging the C# Roslyn compilers code generator extensibility point
to do this.

All you need to do is add a reference to the [Aksio.Cratis.Applications.ProxyGenerator](https://www.nuget.org/packages/Aksio.Cratis.Applications.ProxyGenerator/) NuGet
package and it will at compile time do the magic.

> Note: The projects that hold controllers should all have a reference to this package, since it is running as part of the
> compile steps.

The benefit of this is that you don't have to look at the Swagger API even to know what you have available, the code sits
there directly in the form of a generated proxy object

## Commands

Commands are the things you want to perform. These are represented as **HttpPost** operations on controllers. Any method arguments
are considered properties on the command. Complex types will have its properties added to the command directly as well.
Any of the parameters can be sourced using `[FromRoute]` or `[FromQuery]` and the generated proxy will generate the correct
route template based on whats in `[Route]` in combination with what is defined in `[HttpPost]`.

Take following controller with action in C#:

```csharp
[Route("/api/accounts/debit")]
public class DebitAccounts : Controller
{
    readonly IEventLog _eventLog;

    public DebitAccounts(IEventLog eventLog) => _eventLog = eventLog;

    [HttpPost]
    public Task OpenDebitAccount([FromBody] OpenDebitAccount create) => _eventLog.Append(create.AccountId, new DebitAccountOpened(create.Name, create.Owner));
}
```

The action takes a complex type called `OpenDebitAccount` that looks like this:

```csharp
public record OpenDebitAccount(AccountId AccountId, AccountName Name, PersonId Owner);
```

This will generate:

```typescript
import { Command } from '@aksio/cratis-applications-frontend/commands';

export class OpenDebitAccount extends Command {
    readonly route: string = '/api/accounts/debit';

    accountId!: string;
    name!: string;
    owner!: string;
}
```

While a controller leveraging route parameters:

```csharp
[Route("/api/accounts/debit/{accountId}")]
public class DebitAccount : Controller
{
    readonly IEventLog _eventLog;

    public DebitAccount(IEventLog eventLog) => _eventLog = eventLog;

    [HttpPost("deposit/{amount}")]
    public Task DepositToAccount([FromRoute] AccountId accountId, [FromRoute] double amount) => _eventLog.Append(accountId, new DepositToDebitAccountPerformed(amount));
}
```

It will generate into:

```typescript
import { Command } from '@aksio/cratis-applications-frontend/commands';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/{{accountId}}/deposit/{{amount}}');

export class DepositToAccount extends Command {
    readonly route: string = '/api/accounts/debit/{{accountId}}/deposit/{{amount}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    get requestArguments(): string[] {
        return [
            'accountId',
            'amount',
        ];
    }

    accountId!: string;
    amount!: number;
}
```

## Queries

Queries are the data coming out. These are represents as **HttpGet** operations on controllers and returns either an enumerable
of a specific type or a single item of a type. These can have arguments which will also be part of the proxy objects. The generator will use the
method name as the query name, so remember to name these properly to get meaningful query objects for the frontend.

You can provide parameters to the queries as well. These can either be part of the route or as part of the query string.
(C#: `[FromRoute]` or `[FromQuery]`). The proxy generator will create a type that holds these and becomes compile-time
checked when using the query in the frontend.

Take the following controller action in C#:

```csharp
[HttpGet]
public IEnumerable<DebitAccount> AllAccounts() => _collection.Find(_ => true).ToList();
```

> Note: Return types does not have to be an enumerable, it can also be a single item. However, when returning a collection
> of items - the return type should have a generic parameter of what the actual item type is. This is leveraged during
> the proxy generation.

And the read model in this case looking like:

```csharp
public record DebitAccount(AccountId Id, AccountName Name, PersonId Owner, double Balance);
```

This all gets generated into the following TypeScript code:

```typescript
import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { DebitAccount } from './DebitAccount';
import Handlebars from 'handlebars';


const routeTemplate = Handlebars.compile('/api/accounts/debit');

export class AllAccounts extends QueryFor<DebitAccount> {
    readonly route: string = '/api/accounts/debit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;

    static use(): [QueryResultWithState<DebitAccount>, PerformQuery] {
        return useQuery<DebitAccount, AllAccounts>(AllAccounts);
    }
}
```

> Note: For observable queries, this is a little bit different, read more about how you do this from [backend](../queries.md) and
> [frontend](./queries.md).

The return type for the static method called `.use()` representing a React hook is of type `QueryResultWithState<>`.
This type contains additional information on whether or not the query is being performed or it is finished. This can be helpful for
knowing what to render and one could for instance enable a spinner when the property `isPerforming` is true.

## Getting started

All you need is to reference the following **Aksio.ProxyGenerator** package and configure the property for the output
folder within your **.csproj** file. Lets say you have a structure as below:

```shell
<Your Root Folder>
|
+-- Domain
+-- Read
+-- Web
```

The **Domain** and **Read** projects could typically then have ASP.NET Controllers within them representing commands and queries.
The **Web** project being where you have your web code and the place you want the files to be generated. In the **.csproj**
files for the **Domain** and **Read** you would then add a dependency to the NuGet package and add the following property:

```xml
<PropertyGroup>
    <AksioProxyOutput>$(MSBuildThisFileDirectory)../Web</AksioProxyOutput>
</PropertyGroup>
```

The generator will maintain the folder structure from the source files while generating based on the namespaces of the files.

If you want to not let the type and namespace be the convention for the target folder, you can use the route of the controller
as the folder instead by adding the following property:

```xml
<PropertyGroup>
    <AksioUseRouteAsPath>true</AksioUseRouteAsPath>
</PropertyGroup>
```

The path from the route will be appended to the value of `<AksioProxyOutput/>`. It will strip away any prefix of `/api` in the route.
