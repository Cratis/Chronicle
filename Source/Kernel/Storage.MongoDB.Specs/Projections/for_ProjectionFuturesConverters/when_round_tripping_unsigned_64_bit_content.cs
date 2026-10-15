// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionFuturesConverters;

public class when_round_tripping_unsigned_64_bit_content : Specification
{
    Concepts.Projections.ProjectionFuture _future;
    ProjectionFuture _stored;
    Concepts.Projections.ProjectionFuture _read;
    readonly JsonSerializerOptions _options = new();

    void Establish()
    {
        var content = new ExpandoObject();
        var values = (IDictionary<string, object?>)content;
        values["value"] = ulong.MaxValue;
        values["values"] = new object[] { 1, ulong.MaxValue, "x" };
        _future = new(
            ProjectionFutureId.New(),
            new ProjectionId("projection"),
            new AppendedEvent(EventContext.Empty, content),
            PropertyPath.Root,
            new PropertyPath("children"),
            new PropertyPath("id"),
            new PropertyPath("parentId"),
            new Key("parent", ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
    }

    void Because()
    {
        _stored = _future.ToMongoDB(_options);
        _read = _stored.ToKernel(_options);
    }

    [Fact] void should_store_large_unsigned_values_as_decimal128() => _stored.Event.Content["value"].AsDecimal.ShouldEqual(ulong.MaxValue);
    [Fact] void should_restore_large_unsigned_values() => ((JsonElement)((IDictionary<string, object?>)_read.Event.Content)["value"]!).GetUInt64().ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_the_mixed_array() => JsonElement.DeepEquals(JsonSerializer.SerializeToElement(_read.Event.Content).GetProperty("values"), JsonSerializer.SerializeToElement(_future.Event.Content).GetProperty("values")).ShouldBeTrue();
    [Fact] void should_preserve_the_future_identity() => _read.Id.ShouldEqual(_future.Id);
}
