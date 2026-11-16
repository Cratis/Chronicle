// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_nested_decimal_array_content : Specification
{
    JsonSchema _schema;
    BsonDocument _result;

    async Task Establish() => _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"items":{"type":"array","items":{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}}}}""");

    void Because() => _result = EventContentBson.FromJson("""{"items":[{"amount":1.2500000000000000000000000001},{"amount":null},{}]}""", _schema);

    [Fact] void should_preserve_decimal_precision_inside_arrays() => _result["items"][0]["amount"].AsDecimal128.ShouldEqual(new Decimal128(1.2500000000000000000000000001m));
    [Fact] void should_keep_null_array_members() => _result["items"][1]["amount"].IsBsonNull.ShouldBeTrue();
    [Fact] void should_not_add_absent_properties() => _result["items"][2].AsBsonDocument.ElementCount.ShouldEqual(0);
}
