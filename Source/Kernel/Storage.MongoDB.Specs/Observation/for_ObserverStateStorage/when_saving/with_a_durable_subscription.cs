// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class with_a_durable_subscription : given.an_observer_state_storage
{
    BsonValue _expected;

    void Establish()
    {
        _state = _state with
        {
            DurableSubscription = new(
                ObserverSubscriptionKind.ClientReactor,
                true,
                new(["tag"], "source", "stream"),
                [new("connection-1", "127.0.0.1:11111@1")])
        };
        _expected = _state.ToMongoDB().ToBsonDocument()["durableSubscription"];
    }

    async Task Because() => await _storage.Save(_state, Concepts.EventSequences.EventSequenceId.Log);

    [Fact] void should_set_the_durable_subscription() => _set["durableSubscription"].ShouldEqual(_expected);
    [Fact] void should_store_it_under_a_name_an_older_kernel_does_not_map() => _set.Names.Count(name => name.Equals("durableSubscription", StringComparison.Ordinal)).ShouldEqual(1);
}
