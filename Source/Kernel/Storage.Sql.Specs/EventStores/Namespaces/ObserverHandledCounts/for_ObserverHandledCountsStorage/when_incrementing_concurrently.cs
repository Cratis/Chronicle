// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_ObserverHandledCountsStorage;

public class when_incrementing_concurrently : given.an_observer_handled_counts_storage
{
    const int Workers = 8;
    const int Iterations = 10;
    IReadOnlyDictionary<EventTypeId, EventCount> _result;

    async Task Because()
    {
        await Task.WhenAll(Enumerable.Range(0, Workers).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                await FreshStorage().Increment("observer", "partition", new Dictionary<EventTypeId, EventCount> { ["type-a"] = 1UL });
            }
        })));
        _result = await FreshStorage().GetFor("observer", "partition");
    }

    [Fact] void should_not_lose_any_increment() => _result["type-a"].Value.ShouldEqual((ulong)(Workers * Iterations));
}
