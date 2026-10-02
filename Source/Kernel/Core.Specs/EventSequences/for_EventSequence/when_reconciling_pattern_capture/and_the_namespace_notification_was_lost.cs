// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_the_namespace_notification_was_lost : given.an_event_sequence_with_pattern_capture
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_subscribe_capture_without_a_notification_or_observer_definition() => _captureIsSubscribed.ShouldBeTrue();
    [Fact] async Task should_subscribe_in_the_sequences_namespace() => await _patternCapture.Received(1).Subscribe(EventStore, EventStoreNamespace);
}
