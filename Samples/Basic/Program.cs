// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Basic;
using Cratis.Chronicle;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder
    .SetMinimumLevel(LogLevel.Trace)
    .AddConsole());

using var client = new CratisClient(
        new CratisOptions(
            new CratisUrl("cratis://localhost:35000"),
            loggerFactory: loggerFactory));

var eventStore = client.GetEventStore(Guid.Empty.ToString());
await eventStore.DiscoverAll();
await eventStore.RegisterAll();

async Task AddItemToCart()
{
    await eventStore.EventLog.Append(
        eventSourceId: Guid.NewGuid(),
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

while (true)
{
    Console.WriteLine("\n\n****** Menu *******");
    Console.WriteLine("---------------------");
    Console.WriteLine("I - Add item to cart");
    Console.WriteLine("B - Add a bulk of items to cart");
    Console.WriteLine("M - Add many items to cart");
    Console.WriteLine("---------------------");
    Console.WriteLine("Q - Exit");
    Console.WriteLine("****** Menu *******");
    var key = Console.ReadKey();
    switch (key.Key)
    {
        case ConsoleKey.Q:
            return;

        case ConsoleKey.I:
            await AddItemToCart();
            break;

        case ConsoleKey.B:
            await AddBulkOfItemsToCart();
            break;

        case ConsoleKey.M:
            await AddManyItemsToCart();
            break;
    }
}
