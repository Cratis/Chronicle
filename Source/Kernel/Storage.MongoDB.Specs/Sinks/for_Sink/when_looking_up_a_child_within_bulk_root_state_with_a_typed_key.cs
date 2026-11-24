// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_looking_up_a_child_within_bulk_root_state_with_a_typed_key : given.a_bulk_child_removal_from_all
{
    static readonly Guid _rootId = Guid.Parse("8e98db5c-893f-498c-880d-2b926b72b851");
    Key _result;

    async Task Establish()
    {
        _converter.ToBsonValue(Arg.Any<Key>()).Returns(call => new BsonBinaryData(Guid.Parse(call.Arg<Key>().Value.ToString()!), GuidRepresentation.Standard));
        await Cache(new Key(_rootId, ArrayIndexers.NoIndexers), new { children = new[] { new { childId = "shared" } } }.AsExpandoObject());
        _collection.ClearReceivedCalls();
    }

    async Task Because()
    {
        var result = await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "shared", new Key(_rootId.ToString(), ArrayIndexers.NoIndexers));
        result.TryGetValue(out _result).ShouldBeTrue();
    }

    [Fact] void should_return_the_cached_typed_root_identifier() => _result.Value.ShouldEqual(_rootId);
    [Fact]
    void should_not_query_persisted_state() => _collection.DidNotReceive().FindAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
        Arg.Any<CancellationToken>());
}
