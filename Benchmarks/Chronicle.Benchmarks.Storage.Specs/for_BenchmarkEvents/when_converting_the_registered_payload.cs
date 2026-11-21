// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkEvents;

public class when_converting_the_registered_payload : Specification
{
    IDictionary<string, object?> _mongo = default!;
    IDictionary<string, object?> _sql = default!;
    async Task Because()
    {
        var schema = (await BenchmarkEvents.CreateEventTypes().GetFor(BenchmarkEvents.Selected.Id, BenchmarkEvents.Selected.Generation)).Schema;
        dynamic content = new ExpandoObject();
        content.payload = new string('x', 256);
        var formats = new TypeFormats();
        var mongo = new Cratis.Chronicle.Storage.MongoDB.ExpandoObjectConverter(formats);
        var sql = new Cratis.Chronicle.Json.ExpandoObjectConverter(formats);
        _mongo = mongo.ToExpandoObject(mongo.ToBsonDocument((ExpandoObject)content, schema), schema);
        _sql = sql.ToExpandoObject(sql.ToJsonObject((ExpandoObject)content, schema), schema);
    }
    [Fact] void should_preserve_mongo_payload_shape() => _mongo.Count.ShouldEqual(1);
    [Fact] void should_preserve_sql_payload_shape() => _sql.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_same_payload() => _mongo["payload"].ShouldEqual(_sql["payload"]);
    [Fact] void should_preserve_payload_length() => ((string)_mongo["payload"]!).Length.ShouldEqual(256);
}
