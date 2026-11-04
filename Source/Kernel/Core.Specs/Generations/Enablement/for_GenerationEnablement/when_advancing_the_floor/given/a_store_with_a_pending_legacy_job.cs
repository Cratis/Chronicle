// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer.given;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

using JobStatus = Cratis.Choreography.Jobs.JobStatus;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationEnablement.when_advancing_the_floor.given;

/// <summary>
/// A store with a real floor advancement and a real legacy-work reader over a stopped replay job, with every other
/// enablement condition satisfied. The only thing standing between the store and enablement is the pending legacy job.
/// </summary>
public class a_store_with_a_pending_legacy_job : an_initializer
{
    protected IEventStoreNamespaceStorage _namespace;
    protected GenerationFloorAdvancement _advancement;
    protected GenerationFloorAdvance _request;
    protected GenerationFloorAdvanceResult _advanced;

    void Establish()
    {
        _namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _store.Namespaces.GetAll().Returns([new NamespaceState(new("tenant"), default)]);
        _store.GetNamespace(new("tenant")).Returns(_namespace);
        _namespace.ReplayContexts.GetAll().Returns(_ => Task.FromResult<IReadOnlyList<ReplayContext>>([]));
        _namespace.Observers.GetAll().Returns(_ => Task.FromResult<IEnumerable<ObserverState>>([]));
        var pending = new JobState { Id = new(Guid.NewGuid()), Status = JobStatus.Stopped };
        _namespace.Jobs.GetJobs<IReplayObserver, JobState>().Returns(_ => Task.FromResult<Catch<IImmutableList<JobState>, JobError>>(ImmutableList.Create(pending)));
        _namespace.Jobs.GetJobs<ICatchUpObserver, JobState>().Returns(_ => Task.FromResult<Catch<IImmutableList<JobState>, JobError>>(ImmutableList<JobState>.Empty));

        _legacy = new GenerationLegacyWork(_storage);
        Reset();

        var target = _floor with { MinimumStorageProtocol = new(2), MinimumWriterProtocol = new(2), Revision = 2 };
        _request = new(target, 1, "operator", "advance the floor", new(false, "stale"));
        _store.GenerationFloor.Advance(Arg.Any<GenerationFloorAdvance>()).Returns(new GenerationFloorAdvanced(target));
        _advancement = new(_storage, _membership);
        _storage.ClearReceivedCalls();
        _namespace.ClearReceivedCalls();
    }

    protected async Task AdvanceThenInitialize()
    {
        _advanced = await _advancement.Advance(_name, _request);
        _result = await _initializer.Initialize(_name);
    }
}
