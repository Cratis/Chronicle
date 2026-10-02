// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_subscribed_but_has_not_finished_routing : given.an_event_sequence_with_pattern_capture
{
    void Establish()
    {
        _captureIsSubscribed = true;
        _captureRunningState = ObserverRunningState.Unknown;
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_retry_capture_setup() => await _patternCapture.Received(1).Subscribe(EventStore, EventStoreNamespace);
}
