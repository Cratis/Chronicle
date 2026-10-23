// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_in_primary;

public class and_the_cached_primary_is_replaced_during_a_miss : given.a_sink_observing_a_container
{
    Exception? _error;

    async Task Establish()
    {
        _primaryId = Guid.NewGuid();
        _documents = [new BsonDocument("_id", "account-1")];
        await _sink.FindInPrimaryOrDefault(new Key("account-1", ArrayIndexers.NoIndexers));
        _documents = [];
        _listCollectionsCalls = 0;
        _duringFind = () => _primaryId = Guid.NewGuid();
    }

    async Task Because() => _error = await Catch.Exception(async () =>
        await _sink.FindInPrimaryOrDefault(new Key("missing", ArrayIndexers.NoIndexers)));

    [Fact] void should_refuse_an_ambiguous_lookup() => _error.ShouldBeOfExactType<ReadModelPrimaryIsChanging>();
    [Fact] void should_refresh_the_identity_before_retrying() => _listCollectionsCalls.ShouldEqual(5);
}
