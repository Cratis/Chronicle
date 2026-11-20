// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class with_type_set_control_and_without_a_durable_subscription : given.an_observer_state_storage
{
    void Establish() =>
        _state = _state with
        {
            TypeSetControl = new(
                new(7),
                new("reactor-subscriber", Concepts.Observation.ObserverFilters.None),
                new Dictionary<EventTypeId, ObserverTypeSetMember> { [new("event-a")] = new(new(3), null) },
                [])
        };

    async Task Because() => await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);

    [Fact] void should_clear_the_stored_subscription_inside_the_guarded_pipeline() => _set["durableSubscription"]["$literal"].ShouldEqual(BsonNull.Value);
    [Fact] void should_still_guard_the_type_set_control() => _set["typeSetControl"].AsBsonDocument.Contains("$cond").ShouldBeTrue();
}
