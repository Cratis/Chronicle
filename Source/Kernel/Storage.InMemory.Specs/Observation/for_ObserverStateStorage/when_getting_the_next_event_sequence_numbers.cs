// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage;

public class when_getting_the_next_event_sequence_numbers : Specification
{
    ObserverStateStorage _storage;
    IReadOnlyDictionary<ObserverId, EventSequenceNumber> _forLog;
    IReadOnlyDictionary<ObserverId, EventSequenceNumber> _forOtherSequence;

    void Establish() => _storage = new();

    async Task Because()
    {
        var subscription = new ObserverDurableSubscription(ObserverSubscriptionKind.Projection, true, null, []);
        await _storage.Save(new() { Identifier = "recorded", NextEventSequenceNumber = 42, DurableSubscription = subscription }, EventSequenceId.Log);
        await _storage.Save(new() { Identifier = "not-recorded", NextEventSequenceNumber = 7 }, EventSequenceId.Log);
        _forLog = await _storage.GetNextEventSequenceNumbers(EventSequenceId.Log);
        _forOtherSequence = await _storage.GetNextEventSequenceNumbers(EventSequenceId.Outbox);
    }

    [Fact] void should_return_the_cursor_of_the_observer_that_recorded_a_subscription() => _forLog["recorded"].ShouldEqual((EventSequenceNumber)42);
    [Fact] void should_return_only_observers_that_recorded_one() => _forLog.Count.ShouldEqual(1);
    [Fact] void should_not_return_the_observers_of_another_sequence() => _forOtherSequence.Count.ShouldEqual(0);
}
