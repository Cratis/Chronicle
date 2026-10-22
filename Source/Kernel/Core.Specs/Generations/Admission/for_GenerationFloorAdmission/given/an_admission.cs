// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Generations.Admission.for_GenerationFloorAdmission.given;

public class an_admission : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _first;
    protected IEventStoreStorage _second;
    protected IEventStoreStorage _system;
    protected GenerationFloorAdmission _admission;

    protected static GenerationFloor Floor(uint protocol) => new(new(protocol), new(1), [GenerationCapability.FloorAdmission], new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow, "spec", "admission");

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _first = Substitute.For<IEventStoreStorage>();
        _second = Substitute.For<IEventStoreStorage>();
        _system = Substitute.For<IEventStoreStorage>();
        _first.EventStore.Returns(new EventStoreName("first"));
        _second.EventStore.Returns(new EventStoreName("second"));
        _system.EventStore.Returns(EventStoreName.System);
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>(["first", "second"]));
        _storage.GetEventStore("first").Returns(_first);
        _storage.GetEventStore("second").Returns(_second);
        _storage.GetEventStore(EventStoreName.System).Returns(_system);
        _first.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(Floor(1)));
        _second.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(Floor(1)));
        _system.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(Floor(1)));
        _admission = new(_storage);
    }
}
