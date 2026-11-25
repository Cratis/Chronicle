// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_looking_up_a_child_within_a_root : given.a_sink_observing_a_container
{
    readonly Guid _rootId = Guid.NewGuid();
    Key _root;
    BsonDocument _filter;
    object _matched;
    bool _missing;

    void Establish()
    {
        _primaryId = Guid.NewGuid();
        _root = new Key(_rootId.ToString(), ArrayIndexers.NoIndexers);
        var bsonId = new BsonBinaryData(_rootId, GuidRepresentation.Standard);
        _converter.ToBsonValue(_root).Returns(bsonId);
        _documents = [new BsonDocument("_id", bsonId)];
        _collection.When(collection => collection.FindAsync(
                Arg.Any<FilterDefinition<BsonDocument>>(),
                Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>()))
            .Do(call => _filter = call.Arg<FilterDefinition<BsonDocument>>().Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)));
    }

    async Task Because()
    {
        var result = await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "shared", _root);
        result.TryGetValue(out var key).ShouldBeTrue();
        _matched = key.Value;
        _documents = [];
        _missing = (await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "missing", _root)).HasValue;
    }

    [Fact] void should_return_the_typed_root_identifier() => _matched.ShouldEqual(_rootId);
    [Fact] void should_include_the_root_identifier_in_the_query() => _filter["_id"].AsGuid.ShouldEqual(_rootId);
    [Fact] void should_also_require_the_child_identifier() => _filter["children.childId"].AsString.ShouldEqual("missing");
    [Fact] void should_return_none_when_the_scoped_query_does_not_match() => _missing.ShouldBeFalse();
}
