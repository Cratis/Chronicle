// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCacheKey;

public class when_identifying_keys_with_decimals_and_other_numbers : Specification
{
    static BsonDocument Composite(BsonValue number) => new() { { "amount", new BsonDecimal128(1.00m) }, { "number", number } };

    [Fact] void should_preserve_numeric_types() => SinkCacheKey.For(Composite(new BsonInt32(1))).ShouldNotEqual(SinkCacheKey.For(Composite(new BsonInt64(1))));
}
