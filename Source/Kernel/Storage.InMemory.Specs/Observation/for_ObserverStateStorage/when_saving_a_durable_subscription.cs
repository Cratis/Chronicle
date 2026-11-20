// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage;

public class when_saving_a_durable_subscription : Specification
{
    ObserverStateStorage _storage;
    ObserverDurableSubscription _subscription;
    IReadOnlyDictionary<Concepts.Observation.ObserverId, ObserverDurableSubscription> _forLog;
    IReadOnlyDictionary<Concepts.Observation.ObserverId, ObserverDurableSubscription> _forOtherSequence;
    IReadOnlyDictionary<Concepts.Observation.ObserverId, ObserverDurableSubscription> _afterClearing;

    void Establish()
    {
        _storage = new();
        _subscription = new(ObserverSubscriptionKind.Projection, true, null, []);
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "recorded", DurableSubscription = _subscription }, EventSequenceId.Log);
        await _storage.Save(new() { Identifier = "not-recorded" }, EventSequenceId.Log);
        _forLog = await _storage.GetDurableSubscriptions(EventSequenceId.Log);
        _forOtherSequence = await _storage.GetDurableSubscriptions(EventSequenceId.Outbox);
        await _storage.Save(new() { Identifier = "recorded" }, EventSequenceId.Log);
        _afterClearing = await _storage.GetDurableSubscriptions(EventSequenceId.Log);
    }

    [Fact] void should_return_the_recorded_subscription() => _forLog["recorded"].ShouldEqual(_subscription);
    [Fact] void should_return_only_observers_that_recorded_one() => _forLog.Count.ShouldEqual(1);
    [Fact] void should_not_return_the_observers_of_another_sequence() => _forOtherSequence.ShouldBeEmpty();
    [Fact] void should_return_none_once_it_is_cleared() => _afterClearing.ShouldBeEmpty();
}
