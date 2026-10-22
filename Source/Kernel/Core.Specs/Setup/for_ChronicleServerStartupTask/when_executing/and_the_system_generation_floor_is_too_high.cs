// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_system_generation_floor_is_too_high : given.a_startup_task
{
    Exception _error;
    void Establish()
    {
        var system = Substitute.For<Cratis.Chronicle.Storage.IEventStoreStorage>();
        _storage.GetEventStore(Cratis.Chronicle.Concepts.EventStoreName.System).Returns(system);
        system.GenerationFloor.Get().Returns(new GenerationFloor(new(2), new(1), [], new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow, "spec", "test"));
    }
    async Task Because() => _error = await Catch.Exception(Execute);
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] void should_not_register_event_types() => _eventTypes.DidNotReceiveWithAnyArgs().DiscoverAndRegister(Arg.Any<EventStoreName>());
    [Fact] void should_not_apply_patches() => _patchManager.DidNotReceive().ApplyPatches();
    [Fact] void should_not_register_system_reactors() => _reactors.DidNotReceiveWithAnyArgs().DiscoverAndRegister(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_not_resume_jobs() => _jobsManager.DidNotReceive().Rehydrate();
    [Fact] void should_not_bootstrap_clients() => _bootstrapClientsEnsured.ShouldBeFalse();
}
