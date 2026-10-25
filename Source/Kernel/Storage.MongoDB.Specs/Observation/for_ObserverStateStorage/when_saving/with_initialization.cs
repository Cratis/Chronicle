// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class with_initialization : given.an_observer_state_storage
{
    BsonValue _expected;

    void Establish()
    {
        _state = _state with { Initialization = new(ObserverInitializationState.NotInitialized, ["unfinished job", "unsettled handover"]) };
        _expected = _state.ToMongoDB().ToBsonDocument()["initialization"];
    }

    async Task Because() => await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);

    [Fact] void should_set_the_whole_initialization_record() => _set["initialization"].ShouldEqual(_expected);
}
