// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_generation_floor_storage_is_unavailable : given.a_startup_task
{
    Exception _error;
    void Establish()
    {
        _storage.ReadGenerationFloors().Returns(_ => Task.FromException<IReadOnlyDictionary<EventStoreName, GenerationFloor>>(new IOException("floor unavailable")));
    }
    async Task Because() => _error = await Catch.Exception(Execute);
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_not_register_event_types() => _eventTypes.DidNotReceiveWithAnyArgs().DiscoverAndRegister(Arg.Any<EventStoreName>());
    [Fact] void should_not_apply_patches() => _patchManager.DidNotReceive().ApplyPatchesFor(Arg.Any<Cratis.Chronicle.Concepts.System.SemanticVersion>());
    [Fact] void should_not_register_system_reactors() => _reactors.DidNotReceiveWithAnyArgs().DiscoverAndRegister(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_not_resume_jobs() => _jobsManager.DidNotReceive().Rehydrate();
    [Fact] void should_not_bootstrap_clients() => _bootstrapClientsEnsured.ShouldBeFalse();
}
