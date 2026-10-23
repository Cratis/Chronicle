// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_in_primary;

public class and_the_primary_identity_is_cached : given.a_sink_observing_a_container
{
    ExpandoObject? _result;

    async Task Establish()
    {
        _primaryId = Guid.NewGuid();
        _documents = [new BsonDocument("_id", "account-1")];
        await _sink.FindInPrimaryOrDefault(new Key("account-1", ArrayIndexers.NoIndexers));
        _listCollectionsCalls = 0;
    }

    async Task Because() => _result = await _sink.FindInPrimaryOrDefault(new Key("account-1", ArrayIndexers.NoIndexers));

    [Fact] void should_return_the_primary_instance() => _result.ShouldNotBeNull();
    [Fact] void should_not_probe_collection_state_on_a_hit() => _listCollectionsCalls.ShouldEqual(0);
}
