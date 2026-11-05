// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_a_projection_in_a_never_initialized_store : given.an_observer_with_subscription
{
    Exception _error;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 42UL };
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([]));
    }

    async Task Because() => _error = await Catch.Exception(_observer.Replay);

    [Fact] void should_not_refuse() => _error.ShouldBeNull();
    [Fact] async Task should_start_a_replay_job() => await _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_not_record_receipts() => await _eventStoreStorage.GenerationInitialization.DidNotReceive().TryRecord(Arg.Any<GenerationInitializationReceipt>());
    [Fact] async Task should_not_record_blockers() => await _eventStoreStorage.GenerationInitialization.DidNotReceive().RecordBlocker(Arg.Any<GenerationInitializationBlocker>());
    [Fact] async Task should_not_resolve_blockers() => await _eventStoreStorage.GenerationInitialization.DidNotReceive().ResolveBlocker(Arg.Any<GenerationInitializationBlockerKind>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>());
    [Fact] async Task should_not_advance_the_floor() => await _eventStoreStorage.GenerationFloor.DidNotReceive().Advance(Arg.Any<GenerationFloorAdvance>());
}
