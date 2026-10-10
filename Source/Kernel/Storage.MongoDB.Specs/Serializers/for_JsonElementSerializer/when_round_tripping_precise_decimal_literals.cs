// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers.for_JsonElementSerializer;

public class when_round_tripping_precise_decimal_literals : Specification
{
    BsonDocument _stored;
    JsonElement _read;

    void Because()
    {
        var serializer = new JsonElementSerializer(new JsonSerializerOptions());
        _stored = new BsonDocument();
        using (var writer = new BsonDocumentWriter(_stored))
        {
            serializer.Serialize(
                BsonSerializationContext.CreateRoot(writer),
                default,
                JsonSerializer.Deserialize<JsonElement>("""{"precise":1234567890.123456789012345678,"ordinary":193.58}"""));
        }
        using var reader = new BsonDocumentReader(_stored);
        _read = serializer.Deserialize(BsonDeserializationContext.CreateRoot(reader), default);
    }

    [Fact] void should_store_precise_literals_as_decimal128() => _stored["precise"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_decimal_bits() => decimal.GetBits(_read.GetProperty("precise").GetDecimal()).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
    [Fact] void should_keep_round_tripping_double_literals_as_doubles() => _stored["ordinary"].BsonType.ShouldEqual(BsonType.Double);
}
