// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationAdmission.given;

public class a_provider : Specification
{
    protected EventStoreStorages _registry;
    protected Storage _storage;
    protected GenerationFloorAdmission _admission;

    protected EventStoreName _name = new("orders");

    void Establish()
    {
        var factories = Substitute.For<IInstancesOf<ISinkFactory>>();
        factories.GetEnumerator().Returns(_ => Enumerable.Empty<ISinkFactory>().GetEnumerator());
        _registry = new(factories, Substitute.For<Cratis.Orleans.Storage.IJobsStorage>());
        _storage = new(_registry, Substitute.For<ISystemStorage>());
        _admission = new(_storage);
    }

    protected async Task Advance(uint protocol)
    {
        var floor = new GenerationFloor(new(protocol), new(protocol), [GenerationCapability.FloorAdmission], new(Guid.NewGuid()), 0, default, "", "");
        await _storage.GetEventStore(_name).GenerationFloor.Advance(new(floor, 0, "spec", "floor", new(true, "uniform")));
    }

    void Destroy() => _registry.Dispose();
}
