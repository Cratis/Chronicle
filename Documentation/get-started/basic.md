# Basic

This is a basic tutorial that walks through how to get started with Cratis.
The tutorial expects basic knowledge of C#, .NET and Docker.

To understand the concepts that Cratis is built upon, please read more in
the [concepts section](../concepts).

## Prerequisites

To work with Cratis you'll need the following installed:

- [Docker](https://docs.docker.com/engine/install/)
- [.NET 6.0 or better](https://dotnet.microsoft.com/en-us/download)

## Setup

After making sure you have the prerequisites in place, you need to
setup the server and then create your client.

### Server

You can start the Cratis server, also known as the **Kernel** by doing the
following in a shell.

```shell
docker run -d \
    -p 27017:27017 \
    -p 8080:80 \
    -p 8081:81 \
    --add-host="host.docker.internal:host-gateway" \
    aksioinsurtech/cratis:latest-development
```

This should yield something like the following:

```shell
e04e1c02819a96ae3f85a2b4579cb1cab8623625fc272ece857ef37397dcd322
```

> Note: Cratis is as of version 9.4.3 using an HTTP based protocol. This requires the
> Cratis server to be able to call the client using HTTP as well. Therefor we add the
> `--add-host` option to the `docker run`. This is only for localhost scenarios.
> The client communicates to the server where the client is located, based on configuration
> ASP.NET Core exposes.

### Client

Create a folder for your Cratis client, you could call it "ECommerce".
Then run the following shell command:

```shell
dotnet new web
```

You need to add the Cratis package, do so by running the following in the shell:

```shell
dotnet add package Aksio.Cratis.AspNetCore
```

Open up the `Program.cs` file that was generated and change it to the following:

```c#
var builder = WebApplication.CreateBuilder(args);
builder.UseCratis();    // Adds the necessary Cratis configuration
var app = builder.Build();
app.UseCratis();        // Makes sure to get Cratis client started
app.Run();
````

The code hooks into the ASP.NET Core builders and configures Cratis.
Make note that the first `UseCratis()` method sets up configuration and has an
overload that allows you to specify more configuration, such as what tenancy model
to use (single or multi, single is default).

To verify that everything is working at this stage, run the following in the shell:

```shell
dotnet run
```

You should be seeing something like the following:

```shell
Building...
info: Aksio.Cratis.ClientBuilder[0]
      Configuring Cratis client
info: Aksio.Cratis.ClientBuilder[1]
      Configuring services
info: Aksio.Cratis.ClientBuilder[2]
      Configuring compliance
info: Aksio.Cratis.ClientBuilder[3]
      Using single kernel client @ 'http://localhost:8080/'
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5288
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/einari/Projects/Playground/CratisTutorial
info: Aksio.Cratis.Connections.RestKernelConnection[14]
      Attempting to connect
info: Aksio.Cratis.EventSequences.Outbox.OutboxProjectionsRegistrar[1]
      Registering outbox projections
info: Aksio.Cratis.Observation.ObserversRegistrar[0]
      Registering observers
info: Aksio.Cratis.Reducers.ReducersRegistrar[0]
      Registering reducers
info: Aksio.Cratis.Projections.ProjectionsRegistrar[1]
      Registering projections
info: Aksio.Cratis.Schemas.SchemasConnectionLifecycleParticipant[1]
      Registering event types
info: Aksio.Cratis.Connections.RestKernelConnection[11]
      Connected to Cratis Kernel
info: Aksio.Cratis.Connections.RestKernelConnection[12]
      Setting up client ping
```

Make sure it says `Connected to Cratis Kernel` in the one of log lines.
You can stop the client at this stage (Ctrl+C - break).

## Create your first event

Events are just types that are can either be a `class` or a `record` type.
We recommend using a `record`, as that gives you an immutable type. Events are
not be changed in any way, they represent a state change that happened to your
system.

Lets add an event type called `ItemAddedToCart` by adding a file called `ItemAddedToCart.cs`.
Add the following to it:

```csharp
using Aksio.Cratis.Events;

namespace ECommerce;

[EventType("1e9bbdfb-444f-4b48-9087-8b1b8a1de996")]
public record ItemAddedToCart(string ItemId, int Quantity);
```

The event needs to have a unique identifier.


## Turning into read model

```csharp
namespace ECommerce;

public record Cart(string Id, IEnumerable<CartItem> Items);
public record CartItem(string ItemId, int Quantity);
```


```csharp
using Aksio.Cratis.Events;
using Aksio.Cratis.Reducers;

namespace ECommerce;

[Reducer("2040382a-62ef-4aa1-9d83-7f65aa57e611")]
public class CartReducer : IReducerFor<Cart>
{
    public Task<Cart> ItemAdded(ItemAddedToCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items?.Append(new CartItem(@event.ItemId, @event.Quantity)) ??
                new[] { new CartItem(@event.ItemId, @event.Quantity) }
        });
    }
}
```

Since Cratis uses the configured service container to resolve instances, it relies on there
being a registration of the `CartReducer`. By default, ASP.NET Core does not discover any
types and automatically register them, so you have to manually register it with the `Services`.

Add the following line after the first `app.UseCratis()`.

```csharp
builder.Services.AddTransient<CartReducer>();
```

## Appending the event

The last thing you want to do is to add an endpoint that you can call that appends an
event to the event store. This event will then be consumed by the reducer you wrote and
produce the read model.

Open up the `Program.cs` file. At the bottom of the file before the `app.Run()` call,
add the following code:

```csharp
app.MapPost("/api/cart", async () =>
{
    var eventLog = app.Services.GetRequiredService<IEventLog>();
    await eventLog.Append(Guid.NewGuid(), new ItemAddedToCart(Guid.NewGuid().ToString(), 1));
});
```

The code exposes an API on the `/api/cart` route as a POST action.
Within the handle method, it uses the registered application services (`app.Services`) to get
an instance of the `IEventLog`.

> Note: Since the `IEventLog` is registered with the service container, you can take a dependency
> to `IEventLog` and other Cratis services as a constructor argument. This is for instance very
> helpful if creating a WebAPI controller or similar.

Your entire `Program.cs` should now look like the following:

```csharp
using Aksio.Cratis.Compliance.GDPR;
using Aksio.Cratis.EventSequences;
using ECommerce;

var builder = WebApplication.CreateBuilder(args);
builder.UseCratis();

builder.Services.AddTransient<CartReducer>();

var app = builder.Build();
app.UseCratis();

app.MapPost("/api/cart", async () =>
{
    var eventLog = app.Services.GetRequiredService<IEventLog>();
    await eventLog.Append(Guid.NewGuid(), new ItemAddedToCart(Guid.NewGuid().ToString(), 1));
});

app.Run();
```

Run your program using `dotnet run`.

You should now be able to do a POST action to the newly created API.
This can be done using things like [curl](https://curl.se).

Run the following from your shell (remember to put in the correct port shown in the log output):

```shell
curl -iX POST http://localhost:5288/api/cart
```

You should then see the following:

```shell
HTTP/1.1 200 OK
Content-Length: 0
Date: Mon, 25 Sep 2023 17:42:50 GMT
Server: Kestrel
```

Doing the same using [Postman](https://www.postman.com):

![](./postman-add-cart.png)
