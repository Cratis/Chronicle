// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_materialization_is_adopted_during_finalization : given.a_replay_observer_job
{
    Exception _error;

    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _observer.GetFailedPartitionKeys().Returns(["failed-partition"]);
        static GenerationInitializationReceipt Receipt(GenerationInitializationStep step) => new(new("orders"), step, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow);
        IReadOnlyList<GenerationInitializationReceipt> quiescing = [Receipt(GenerationInitializationStep.FloorVerified), Receipt(GenerationInitializationStep.LegacyWorkReconciled)];
        IReadOnlyList<GenerationInitializationReceipt> adopted = [Receipt(GenerationInitializationStep.MaterializationAdopted)];
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult(quiescing), Task.FromResult(adopted));
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.PrepareStepsForTesting(_request);
        _error = await Catch.Exception(_job.CompleteForTesting);
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GenerationInitializationReplayNotSupported>();
    [Fact] async Task should_release_the_observer_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedReplay();
    [Fact] void should_not_report_an_incomplete_replay() => _observer.DidNotReceive().Replayed(Arg.Any<EventSequenceNumber>());
    [Fact] void should_not_report_a_successful_replay() => _observer.DidNotReceive().ReplayedSuccessfullySince(Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
    [Fact] void should_not_promote_the_rebuilt_read_model() => _replayServiceClient.DidNotReceive().EndReplayFor(Arg.Any<ObserverDetails>());
}
