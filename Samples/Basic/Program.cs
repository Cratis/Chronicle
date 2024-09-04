// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Basic;
using Cratis.Chronicle;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

using var loggerFactory = LoggerFactory.Create(builder => builder
    .SetMinimumLevel(LogLevel.Trace)
    .AddConsole());

IEventStore eventStore = null!;

var random = new Random();

var carts = new string[]
{
    "6c10b7f4-8da3-4ecd-bb09-84b4915c26f7",
    "023e2719-9410-481d-8c40-a78555575c09",
    "966abc16-b881-42e6-95db-4422773d5131",
    "f5bb4d9c-c5bb-42de-aad0-5f263f15caac"
};

async Task AddItemToCart()
{
    await eventStore.EventLog.Append(
        eventSourceId: carts[random.Next(0, carts.Length)],
        new ItemAddedToCart(
            PersonId: new(Guid.NewGuid()),
            MaterialId: new(Guid.NewGuid()),
            Quantity: 1,
            Price: 42,
            Description: "This is a description"));
}

async Task AddBulkOfItemsToCart()
{
    var events = Enumerable.Range(1, 1000).Select(_ => new ItemAddedToCart(
        PersonId: new(Guid.NewGuid()),
        MaterialId: new(Guid.NewGuid()),
        Quantity: 1,
        Price: 42,
        Description: "This is a description"));

    foreach (var @event in events)
    {
        await eventStore.EventLog.Append(
            eventSourceId: Guid.NewGuid(),
            @event);
    }
}

async Task AddManyItemsToCart()
{
    var events = Enumerable.Range(1, 1000).Select(_ => new ItemAddedToCart(
        PersonId: new(Guid.NewGuid()),
        MaterialId: new(Guid.NewGuid()),
        Quantity: 1,
        Price: 42,
        Description: "This is a description"));

    await eventStore.EventLog.AppendMany(
        eventSourceId: Guid.NewGuid(),
        events);
}

var app = builder.Build();

app.MapGet("/init", async () =>
{
    var client = new ChronicleClient(
            new ChronicleOptions(
                new ChronicleUrl("chronicle://localhost:35000"),
                loggerFactory: loggerFactory));

    eventStore = client.GetEventStore("basic");
    await eventStore.DiscoverAll();
    await eventStore.RegisterAll();
});

app.MapGet(
    "/",
    async () => await AddItemToCart());

await app.RunAsync();


// while (true)
// {
//     Console.WriteLine("\n\n****** Menu *******");
//     Console.WriteLine("---------------------");
//     Console.WriteLine("I - Add item to cart");
//     Console.WriteLine("B - Add a bulk of items to cart");
//     Console.WriteLine("M - Add many items to cart");
//     Console.WriteLine("A - Do stuff to Aggregate");
//     Console.WriteLine("---------------------");
//     Console.WriteLine("Q - Exit");
//     Console.WriteLine("****** Menu *******");
//     var key = Console.ReadKey();
//     switch (key.Key)
//     {
//         case ConsoleKey.Q:
//             return;

//         case ConsoleKey.I:
//             await AddItemToCart();
//             break;

//         case ConsoleKey.B:
//             await AddBulkOfItemsToCart();
//             break;

//         case ConsoleKey.M:
//             await AddManyItemsToCart();
//             break;

//         case ConsoleKey.A:
//             break;
//     }
// }
