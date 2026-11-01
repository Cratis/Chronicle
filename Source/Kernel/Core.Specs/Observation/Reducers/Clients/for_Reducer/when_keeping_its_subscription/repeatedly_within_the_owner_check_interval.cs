// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class repeatedly_within_the_owner_check_interval : given.a_reducer_grain_with_a_stored_definition
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(true);

    async Task Because()
    {
        await _grain.KeepSubscription(_connectionDefinition);
        _clock.Advance(TimeSpan.FromSeconds(2));
        await _grain.KeepSubscription(_connectionDefinition);
        _clock.Advance(TimeSpan.FromSeconds(27));
        await _grain.KeepSubscription(_connectionDefinition);
    }

    [Fact] async Task should_read_the_stored_definition_once() => await _storage.GetEventStore("the-event-store").Observers.Received(1).Get(Arg.Any<ObserverId>());
    [Fact] async Task should_keep_checking_with_the_observer() => await _observer.Received(3).KnowsClient(_client.ConnectionId);
}
