// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_primary_is_changing : given.a_composite_key_lookup
{
    void Establish()
    {
        _primaryId = null;
        _promotingExists = true;
        _documents = [new BsonDocument("_id", _id)];
        _duringListCollections = () =>
        {
            if (_listCollectionsCalls == 3)
            {
                _primaryId = Guid.NewGuid();
                _promotingExists = false;
            }
        };
    }
    async Task Because() => _result = await Lookup();
    [Fact] void should_retry_and_find_the_promoted_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_stop_after_the_third_attempt() => _listCollectionsCalls.ShouldEqual(3);
}
