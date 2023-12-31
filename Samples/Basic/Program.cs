// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Aksio.Cratis;
using Aksio.Cratis.Configuration;
using Basic;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder
    .SetMinimumLevel(LogLevel.Trace)
    .AddConsole());

using var client = new CratisClient(
        new CratisOptions(
            new CratisUrl("cratis://localhost:35000"),
            KernelConnectivity.Default,
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

while (true)
{
    Console.WriteLine("\n\n****** Menu *******");
    Console.WriteLine("---------------------");
    Console.WriteLine("I - Add item to cart");
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
    }
}
