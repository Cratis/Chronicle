// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class without_a_durable_subscription : given.an_observer_state_storage
{
    async Task Because() => await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);

    [Fact] void should_clear_the_stored_subscription() => _set["durableSubscription"].ShouldEqual(BsonNull.Value);
}
