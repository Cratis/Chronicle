// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_no_generation_floor_exists : given.a_startup_task
{
    IEventStoreStorage _systemStorage;

    void Establish()
    {
        _systemStorage = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(EventStoreName.System).Returns(_systemStorage);
        _eventStoreStorage.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(null));
        _systemStorage.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(null));
    }

    Task Because() => Execute();

    [Fact] void should_admit_both_stores_before_applying_patches() => Received.InOrder(() =>
    {
        _eventStoreStorage.GenerationFloor.Get();
        _systemStorage.GenerationFloor.Get();
        _patchManager.ApplyPatches();
    });

    [Fact] void should_still_apply_patches() => _patchManager.Received(1).ApplyPatches();
    [Fact] void should_not_write_the_event_store_floor_without_opt_in() => _eventStoreStorage.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
    [Fact] void should_not_write_the_system_store_floor_without_opt_in() => _systemStorage.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
}
