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

public class when_restoring_decimal128_content : Specification
{
    Concepts.Projections.ProjectionFuture _read;

    void Because()
    {
        var options = new JsonSerializerOptions();
        var future = new Concepts.Projections.ProjectionFuture(ProjectionFutureId.New(), new ProjectionId("projection"), new AppendedEvent(EventContext.Empty, new ExpandoObject()), PropertyPath.Root, new PropertyPath("children"), new PropertyPath("id"), new PropertyPath("parentId"), new Key("parent", ArrayIndexers.NoIndexers), DateTimeOffset.UtcNow);
        var stored = future.ToMongoDB(options);
        stored.Event.Content["amount"] = new BsonDecimal128(193.58m);
        stored.Event.Content["precise"] = new BsonDecimal128(1234567890.123456789012345678m);
        _read = stored.ToKernel(options);
    }

    [Fact] void should_preserve_amount_bits() => decimal.GetBits(((JsonElement)((IDictionary<string, object?>)_read.Event.Content)["amount"]!).GetDecimal()).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits(((JsonElement)((IDictionary<string, object?>)_read.Event.Content)["precise"]!).GetDecimal()).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
