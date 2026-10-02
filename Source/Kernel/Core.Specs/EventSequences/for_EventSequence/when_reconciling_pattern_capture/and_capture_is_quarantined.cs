// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_quarantined : given.an_event_sequence_with_a_capture_observer
{
    async Task Establish()
    {
        await SubscribeCapture();
        await _captureObserver.TransitionTo<QuarantinedObserver>();
        await _captureObserver.Unsubscribe();
        _captureState.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_not_bypass_quarantine() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_preserve_the_quarantine_marker() => (await _captureObserver.GetState()).RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_not_write_recovery_state() => await _captureState.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_start_setup() => await _jobsManager.DidNotReceive().GetAllJobs();
}
