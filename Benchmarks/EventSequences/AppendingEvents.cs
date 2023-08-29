// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;
using Benchmark.Model;

namespace Benchmarks.EventSequences;

public class AppendingEvents : EventLogJob
{
    protected override IEnumerable<Type> EventTypes => new[]
    {
        typeof(ItemAddedToCart),
        typeof(ItemRemovedFromCart),
        typeof(QuantityAdjustedForItemInCart),
    };

    public void SetupEvents()
    {
    }

    [Benchmark]
    [InvocationCount(1)]
    public Task SingleEvent() => Perform(async eventSequence =>
    {
        await (eventSequence?.Append(
           Guid.NewGuid().ToString(),
           typeof(ItemAddedToCart).GetEventType(),
           await SerializeEvent(new ItemAddedToCart(PersonId.New(), MaterialId.New(), 1)),
           GlobalVariables.BenchmarkCausation,
           Identity.System) ?? Task.CompletedTask);
    });
}
