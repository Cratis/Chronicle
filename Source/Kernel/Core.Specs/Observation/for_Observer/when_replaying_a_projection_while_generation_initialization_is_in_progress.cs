// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_a_projection_while_generation_initialization_is_in_progress : given.an_observer_with_subscription
{
    readonly given.RecordingObserverLogger _recordedLogs = new();
    Exception _error;

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 42UL };
        _eventStoreStorage.GenerationInitialization.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow), new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.LegacyWorkReconciled, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
    }

    async Task Because() => _error = await Catch.Exception(_observer.Replay);

    [Fact] void should_explain_the_refusal() => _error.Message.ShouldEqual(GenerationInitializationReplayNotSupported.StartRefusedReason);
    [Fact] void should_preserve_the_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_record_a_replay() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] async Task should_not_start_a_job() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_leave_the_replay_state() => ((await _observer.GetCurrentState()) is Replay).ShouldBeFalse();
    [Fact] void should_not_be_replaying() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Replaying);
    [Fact] void should_release_the_refused_replay() => _recordedLogs.Warnings.Any(_ => _.StartsWith("Released a refused replay", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_record_the_refusal() => _recordedLogs.Warnings.Any(_ => _.Contains(GenerationInitializationReplayNotSupported.StartRefusedReason, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] async Task should_not_reset_the_handled_counts() => await _observerHandledCountsStorage.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
}
