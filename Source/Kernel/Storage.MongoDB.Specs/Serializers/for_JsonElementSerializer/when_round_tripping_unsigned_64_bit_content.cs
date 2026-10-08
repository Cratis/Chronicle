// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers.for_JsonElementSerializer;

public class when_round_tripping_unsigned_64_bit_content : Specification
{
    JsonElementSerializer _serializer;
    JsonElement _content;
    BsonDocument _stored;
    JsonElement _read;

    void Establish()
    {
        _serializer = new(new JsonSerializerOptions());
        _content = JsonSerializer.Deserialize<JsonElement>("""
            {"value":18446744073709551615,"values":[1,18446744073709551615,"x",{"a":[1]}]}
            """);
        _stored = new BsonDocument();
    }

    void Because()
    {
        using var writer = new BsonDocumentWriter(_stored);
        _serializer.Serialize(BsonSerializationContext.CreateRoot(writer), default, _content);
        using var reader = new BsonDocumentReader(_stored);
        _read = _serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), default);
    }

    [Fact] void should_store_large_unsigned_values_as_decimal128() => _stored["value"].AsDecimal.ShouldEqual(ulong.MaxValue);
    [Fact] void should_restore_large_unsigned_values() => _read.GetProperty("value").GetUInt64().ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_the_mixed_array() => JsonElement.DeepEquals(_read.GetProperty("values"), _content.GetProperty("values")).ShouldBeTrue();
}
