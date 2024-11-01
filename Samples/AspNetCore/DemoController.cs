// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Mvc;
using Shared.Carts;
using Shared.Orders;

namespace AspNetCore;

[Route("/api/demo")]
public class DemoController(IEventLog eventLog, IAggregateRootFactory aggregateRootFactory) : ControllerBase
{
    [HttpPost("append-event")]
    public async Task AppendEvent()
    {
        await eventLog.Append(
            eventSourceId: Guid.NewGuid(),
            new ItemAddedToCart(
                PersonId: new(Guid.NewGuid()),
                MaterialId: new(Guid.NewGuid()),
                Quantity: 1,
                Price: 42,
                Description: "This is a description"));
    }

    [HttpPost("append-bulk")]
    public async Task AppendBulk()
    {
        var events = Enumerable.Range(1, 10).Select(_ => new ItemAddedToCart(
               PersonId: new(Guid.NewGuid()),
               MaterialId: new(Guid.NewGuid()),
               Quantity: 1,
               Price: 42,
               Description: "This is a description"));

        foreach (var @event in events)
        {
            await eventLog.Append(
                eventSourceId: Guid.NewGuid(),
                @event);
        }
    }

    [HttpPost("append-many")]
    public async Task AppendMany()
    {
        var events = Enumerable.Range(1, 10).Select(_ => new ItemAddedToCart(
            PersonId: new(Guid.NewGuid()),
            MaterialId: new(Guid.NewGuid()),
            Quantity: 1,
            Price: 42,
            Description: "This is a description"));

        await eventLog.AppendMany(
            eventSourceId: Guid.NewGuid(),
            events);
    }

    [HttpPost("append-aggregate-root")]
    public async Task AppendThroughAggregateRoot()
    {
        var order = await aggregateRootFactory.Get<Order>("ca66e387-fc9f-45ae-a059-5d0e37e604cd");
        await order.DoStuff();
    }
}
