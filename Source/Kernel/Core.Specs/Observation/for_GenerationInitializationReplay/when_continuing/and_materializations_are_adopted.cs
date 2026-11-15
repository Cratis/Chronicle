// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Observation.for_GenerationInitializationReplay.when_continuing;

public class and_materializations_are_adopted : Specification
{
    IStorage _storage;
    IGenerationInitializationStorage _audit;
    readonly ObserverKey _key = new("projection", new("orders"), EventStoreNamespaceName.Default, EventSequenceId.Log);

    void Establish()
    {
        _state = new() { Status = JobStatus.Running };
        _audit = Substitute.For<IGenerationInitializationStorage>();
        _audit.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.MaterializationAdopted, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
        var store = Substitute.For<IEventStoreStorage>();
        store.GenerationInitialization.Returns(_audit);
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
    }

    JobState _state;
    Exception _error;
    int _persisted;
    async Task Because() => _error = await Catch.Exception(() => GenerationInitializationReplay.EnsureContinueAllowed(_storage, new ReplayObserverRequest(_key, ObserverType.Projection, []), _state, () =>
    {
        _persisted++;
        return Task.CompletedTask;
    }));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GenerationInitializationReplayNotSupported>();
    [Fact] void should_fail_the_job() => _state.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_persist_the_failure_once() => _persisted.ShouldEqual(1);
    [Fact] void should_keep_the_reason() => _state.StatusChanges.Single().ExceptionMessages.Single().ShouldEqual(GenerationInitializationReplayNotSupported.ContinueRefusedReason);
}
