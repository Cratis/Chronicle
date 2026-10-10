// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers.for_JsonElementSerializer;

public class when_restoring_decimal128_content : Specification
{
    JsonElement _read;

    void Because()
    {
        using var reader = new BsonDocumentReader(new BsonDocument { ["amount"] = new BsonDecimal128(193.58m), ["precise"] = new BsonDecimal128(1234567890.123456789012345678m) });
        _read = new JsonElementSerializer(new JsonSerializerOptions()).Deserialize(BsonDeserializationContext.CreateRoot(reader), default);
    }

    [Fact] void should_preserve_amount_bits() => decimal.GetBits(_read.GetProperty("amount").GetDecimal()).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits(_read.GetProperty("precise").GetDecimal()).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
