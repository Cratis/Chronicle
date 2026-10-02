// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_the_capture_observer_has_no_subscription : given.an_event_sequence_with_a_capture_observer
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_restore_the_subscription() => (await _captureObserver.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_resume_observing() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_preserve_the_next_sequence_number() => (await _captureObserver.GetState()).NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
}
