// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCacheKey;

public class when_grouping_identities : Specification
{
    static readonly BsonDocument _withoutDecimal = new() { { "first", "tenant-1" }, { "second", "entity-1" } };

    [Fact] void should_use_the_key_itself_without_decimals() => SinkCacheKey.Identity(_withoutDecimal).ShouldBeSame(_withoutDecimal);
    [Fact] void should_match_equivalent_decimal_keys() => SinkCacheKey.Identity(new BsonDecimal128(1.00m)).ShouldEqual(SinkCacheKey.Identity(new BsonDecimal128(1.0m)));
    [Fact] void should_not_match_a_string_holding_the_decimal_identity() => SinkCacheKey.Identity(new BsonString(SinkCacheKey.For(new BsonDecimal128(1m)))).ShouldNotEqual(SinkCacheKey.Identity(new BsonDecimal128(1m)));
    [Fact] void should_give_the_cache_identity_of_a_decimal_key() => SinkCacheKey.ForIdentity(SinkCacheKey.Identity(new BsonDecimal128(1.00m))).ShouldEqual(SinkCacheKey.For(new BsonDecimal128(1.0m)));
    [Fact] void should_give_the_cache_identity_of_a_key_without_decimals() => SinkCacheKey.ForIdentity(SinkCacheKey.Identity(_withoutDecimal)).ShouldEqual(_withoutDecimal.ToString());
}
