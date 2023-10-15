// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
await eventStore.EventLog.Append(
    eventSourceId: Guid.NewGuid(),
    new ItemAddedToCart(
        PersonId: new(Guid.NewGuid()),
        MaterialId: new(Guid.NewGuid()),
        Quantity: 1));

Console.WriteLine("Press a key to exit...");
Console.ReadKey();
