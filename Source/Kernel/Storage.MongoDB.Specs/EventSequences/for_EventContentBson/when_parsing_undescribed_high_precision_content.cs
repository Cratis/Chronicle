// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_undescribed_high_precision_content : Specification
{
    const string Content = """{"amount":0.1234567890123456789012345678,"free":{"amount":0.1234567890123456789012345678},"values":[0.1234567890123456789012345678]}""";
    BsonDocument _fast;
    BsonDocument _slow;

    void Because()
    {
        _fast = EventContentBson.FromJson(Content, JsonSchema.FromJson("""{"type":"object","properties":{"known":{"type":"number"}}}"""));
        _slow = EventContentBson.FromJson(Content, JsonSchema.FromJson("""{"type":"object","properties":{"known":{"type":"number"},"unused":{"type":"number","format":"decimal"}}}"""));
    }

    [Fact] void should_produce_the_same_content_on_both_paths() => _fast.ShouldEqual(_slow);
    [Fact] void should_preserve_the_undescribed_decimal_exactly() => _fast["amount"].AsDecimal128.ShouldEqual(new Decimal128(0.1234567890123456789012345678m));
    [Fact] void should_preserve_free_form_object_members_exactly() => _fast["free"]["amount"].ShouldEqual(_fast["amount"]);
    [Fact] void should_preserve_free_form_array_items_exactly() => _fast["values"][0].ShouldEqual(_fast["amount"]);
}
