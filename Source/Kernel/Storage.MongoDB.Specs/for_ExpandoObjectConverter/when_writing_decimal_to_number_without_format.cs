// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_writing_decimal_to_number_without_format : Specification
{
    BsonDocument _result;

    void Because()
    {
        dynamic content = new ExpandoObject();
        content.amount = 193.58m;
        content.ratio = 193.58d;
        _result = new ExpandoObjectConverter(new TypeFormats()).ToBsonDocument((ExpandoObject)content,
            JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number"},"ratio":{"type":"number"}}}"""));
    }

    [Fact] void should_store_decimal128() => _result["amount"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_keep_doubles() => _result["ratio"].BsonType.ShouldEqual(BsonType.Double);
}
