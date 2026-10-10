// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionFuturesConverters;

public class when_round_tripping_precise_decimal_content : Specification
{
    ProjectionFuture _stored;
    Concepts.Projections.ProjectionFuture _read;

    void Because()
    {
        var options = new JsonSerializerOptions();
        dynamic content = new ExpandoObject();
        content.precise = 1234567890.123456789012345678m;
        var future = new Concepts.Projections.ProjectionFuture(ProjectionFutureId.New(), new ProjectionId("projection"), new AppendedEvent(EventContext.Empty, (ExpandoObject)content), PropertyPath.Root, new PropertyPath("children"), new PropertyPath("id"), new PropertyPath("parentId"), new Key("parent", ArrayIndexers.NoIndexers), DateTimeOffset.UtcNow);
        _stored = future.ToMongoDB(options);
        _read = _stored.ToKernel(options);
    }

    [Fact] void should_store_deferred_decimals_as_decimal128() => _stored.Event.Content["precise"].BsonType.ShouldEqual(BsonType.Decimal128);
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits(((JsonElement)((IDictionary<string, object?>)_read.Event.Content)["precise"]!).GetDecimal()).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
