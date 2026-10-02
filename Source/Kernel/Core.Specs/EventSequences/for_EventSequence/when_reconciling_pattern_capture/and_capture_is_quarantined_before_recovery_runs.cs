// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_quarantined_before_recovery_runs : given.an_event_sequence_with_a_capture_observer
{
    bool _failedWrite;
    bool _recoveryWasRequested;

    async Task Establish()
    {
        _captureState.WriteStateAsync().Returns(_ =>
        {
            if (!_failedWrite && _captureState.State.RunningState == ObserverRunningState.Unknown)
            {
                _failedWrite = true;
                return Task.FromException(new TimeoutException());
            }

            return Task.CompletedTask;
        });
        (await Catch.Exception(SubscribeCapture)).ShouldBeOfExactType<TimeoutException>();
        _patternCapture.Configure().RecoverSubscription(EventStore, EventStoreNamespace).Returns(async _ =>
        {
            // Another observer turn quarantines it after reconciliation requests recovery, before that turn runs.
            _recoveryWasRequested = true;
            await _captureObserver.TransitionTo<QuarantinedObserver>();
            _captureState.ClearReceivedCalls();
            await RecoverCapture();
        });
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_have_requested_recovery() => _recoveryWasRequested.ShouldBeTrue();
    [Fact] async Task should_keep_the_quarantined_state_machine() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_keep_the_quarantine_marker() => (await _captureObserver.GetState()).RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_preserve_progress() => (await _captureObserver.GetState()).NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] async Task should_not_write_any_recovery_state() => await _captureState.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_never_use_ordinary_subscribe_for_reconciliation() => await _patternCapture.DidNotReceive().Subscribe(EventStore, EventStoreNamespace);
}
