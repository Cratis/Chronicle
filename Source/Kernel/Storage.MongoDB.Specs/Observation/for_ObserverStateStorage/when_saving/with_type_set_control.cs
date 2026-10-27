// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class with_type_set_control : given.an_observer_state_storage
{
    BsonValue _expected;

    void Establish()
    {
        _state = _state with
        {
            TypeSetControl = new(
                new(7),
                new("reactor-subscriber", Concepts.Observation.ObserverFilters.None),
                new Dictionary<EventTypeId, ObserverTypeSetMember> { [new("event-a")] = new(new(3), null) },
                [])
        };
        _expected = _state.ToMongoDB().ToBsonDocument()["typeSetControl"];
    }

    async Task Because() => await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);

    [Fact] void should_set_the_whole_control_atomically() => _set["typeSetControl"]["$cond"][1]["$literal"].ShouldEqual(_expected);
    [Fact] void should_not_set_individual_members() => _set.Names.Any(name => name.StartsWith("typeSetControl.", StringComparison.Ordinal)).ShouldBeFalse();
}
