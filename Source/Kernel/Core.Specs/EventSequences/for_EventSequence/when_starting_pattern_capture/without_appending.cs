// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_starting_pattern_capture;

public class without_appending : given.an_event_sequence
{
    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_reconcile_an_empty_namespace() => _patternCapture.Received(1).RecoverSubscription(EventStore, EventStoreNamespace);
    [Fact] void should_keep_the_reconciliation_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
