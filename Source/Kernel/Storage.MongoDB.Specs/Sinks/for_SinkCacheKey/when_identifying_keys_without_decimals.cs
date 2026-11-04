// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCacheKey;

public class when_identifying_keys_without_decimals : Specification
{
    [Fact] void should_use_the_string_form_of_a_string() => AssertStringForm(new BsonString("tenant-1"));
    [Fact] void should_use_the_string_form_of_an_int64() => AssertStringForm(new BsonInt64(42));
    [Fact] void should_use_the_string_form_of_a_guid() => AssertStringForm(Guid.NewGuid().ToBsonValue());
    [Fact] void should_use_the_string_form_of_composite_strings() => AssertStringForm(new BsonDocument { { "first", "tenant-1" }, { "second", "entity-1" } });
    [Fact] void should_use_the_string_form_of_an_array() => AssertStringForm(new BsonArray { "a", 1L });

    static void AssertStringForm(BsonValue value) => SinkCacheKey.For(value).ShouldEqual(value.ToString());
}
