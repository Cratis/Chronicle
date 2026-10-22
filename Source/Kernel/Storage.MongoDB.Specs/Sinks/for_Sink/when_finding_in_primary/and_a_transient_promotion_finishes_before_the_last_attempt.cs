// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_in_primary;

public class and_a_transient_promotion_finishes_before_the_last_attempt : given.a_sink_observing_a_container
{
    Exception? _error;
    ExpandoObject? _result;

    void Establish()
    {
        _promotingExists = true;
        _documents = [new BsonDocument("_id", "account-1")];
        _duringListCollections = () =>
        {
            if (_listCollectionsCalls == 3)
            {
                _primaryId = Guid.NewGuid();
                _promotingExists = false;
            }
        };
    }

    async Task Because() => _error = await Catch.Exception(async () =>
        _result = await _sink.FindInPrimaryOrDefault(new Key("account-1", ArrayIndexers.NoIndexers)));

    [Fact] void should_retry_without_propagating_the_transient_failure() => _error.ShouldBeNull();
    [Fact] void should_return_the_promoted_instance() => _result.ShouldNotBeNull();
    [Fact] void should_stop_after_the_successful_third_attempt() => _listCollectionsCalls.ShouldEqual(3);
}
