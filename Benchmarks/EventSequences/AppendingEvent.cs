// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;
using Benchmark.Model;

namespace Benchmarks.EventSequences;

public class AppendingEvent : EventLogJob
{
    protected override IEnumerable<Type> EventTypes => new[]
    {
        typeof(ItemAddedToCart)
    };

    [Benchmark]
    [InvocationCount(1)]
    public Task Single() => Perform(async eventSequence =>
    {
        await eventSequence.Append(
           "5753c26b-8f09-4e29-8f1e-2c4518d42260",
           typeof(ItemAddedToCart).GetEventType(),
           SerializeEvent(new ItemAddedToCart(PersonId.New(), MaterialId.New(), 1)),
           GlobalVariables.BenchmarkCausation,
           Identity.System);
    });
}
