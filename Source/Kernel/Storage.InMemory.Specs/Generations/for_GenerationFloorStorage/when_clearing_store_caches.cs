// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationFloorStorage;

public class when_clearing_store_caches : Specification
{
    EventStoreStorages _stores;
    ulong _revision;
    int _history;
    async Task Establish()
    {
        _stores = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<Orleans.Storage.IJobsStorage>());
        var floor = new GenerationFloor(new(1), new(1), [], new(Guid.NewGuid()), 0, DateTimeOffset.MinValue, "", "");
        await _stores.GetOrCreate("orders").GenerationFloor.Advance(new(floor, 0, "spec", "test", new(true, "spec")));
    }
    async Task Because()
    {
        _stores.Clear();
        var floor = _stores.GetOrCreate("orders").GenerationFloor;
        _revision = (await floor.Get()).Revision;
        _history = (await floor.GetHistory()).Count;
    }
    [Fact] void should_keep_the_floor() => _revision.ShouldEqual(1UL);
    [Fact] void should_keep_the_audit() => _history.ShouldEqual(1);
    void Destroy() => _stores.Dispose();
}
