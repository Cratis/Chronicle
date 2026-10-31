// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Recommendations.for_RecommendationStateSerializer;

public class when_round_tripping_the_ignored_flag : Specification
{
    RecommendationStateSerializer _serializer;
    BsonDocument _stored;
    RecommendationState _ignored;
    RecommendationState _legacy;

    void Establish()
    {
        _serializer = new();
        _stored = new BsonDocument();
    }

    void Because()
    {
        using (var writer = new BsonDocumentWriter(_stored))
        {
            _serializer.Serialize(BsonSerializationContext.CreateRoot(writer), default, new RecommendationState { Id = RecommendationId.New(), IsIgnored = true });
        }

        using (var reader = new BsonDocumentReader(_stored))
        {
            _ignored = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), default);
        }

        var legacyDocument = (BsonDocument)_stored.DeepClone();
        legacyDocument.Remove("isIgnored");
        using var legacyReader = new BsonDocumentReader(legacyDocument);
        _legacy = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(legacyReader), default);
    }

    [Fact] void should_store_the_flag() => _stored["isIgnored"].AsBoolean.ShouldBeTrue();
    [Fact] void should_restore_the_flag() => _ignored.IsIgnored.ShouldBeTrue();
    [Fact] void should_read_a_document_without_the_flag_as_not_ignored() => _legacy.IsIgnored.ShouldBeFalse();
}
