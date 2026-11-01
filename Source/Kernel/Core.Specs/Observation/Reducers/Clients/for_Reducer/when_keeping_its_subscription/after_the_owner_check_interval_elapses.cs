// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class after_the_owner_check_interval_elapses : given.a_reducer_with_a_newer_owner
{
    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(true);

        // The first check finds the definition on this registration's own sequence; a newer client claims it afterwards.
        _storage.GetEventStore(_ownerKey.EventStore).Observers.Get(_connectionDefinition.Identifier).Returns(
            new ObserverDefinition { Identifier = _connectionDefinition.Identifier, EventSequenceId = EventSequenceId.Log },
            new ObserverDefinition { Identifier = _connectionDefinition.Identifier, EventSequenceId = _ownerKey.EventSequenceId });
    }

    async Task Because()
    {
        await _grain.KeepSubscription(_connectionDefinition);
        _clock.Advance(TimeSpan.FromSeconds(2));
        await _grain.KeepSubscription(_connectionDefinition);
        _clock.Advance(TimeSpan.FromSeconds(28));
        await _grain.KeepSubscription(_connectionDefinition);
    }

    [Fact] async Task should_read_the_stored_definition_again() => await _storage.GetEventStore(_ownerKey.EventStore).Observers.Received(2).Get(_connectionDefinition.Identifier);
    [Fact] void should_wait_in_standby() => Subscribed.ShouldBeFalse();
    [Fact] async Task should_leave_the_observer_on_its_own_sequence() => await _observer.Received(1).UnsubscribeIfMatchesClient(_client.ConnectionId);
    [Fact] void should_warn_once() => ShouldWarnOnce();
}
