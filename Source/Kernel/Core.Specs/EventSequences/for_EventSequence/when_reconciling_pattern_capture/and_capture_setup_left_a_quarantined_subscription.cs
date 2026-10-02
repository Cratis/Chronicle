// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_setup_left_a_quarantined_subscription : given.an_event_sequence_with_pattern_capture
{
    void Establish()
    {
        _captureIsSubscribed = true;
        _captureRunningState = ObserverRunningState.Quarantined;
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_not_bypass_quarantine() => await _patternCapture.DidNotReceive().RecoverSubscription(EventStore, EventStoreNamespace);
}
