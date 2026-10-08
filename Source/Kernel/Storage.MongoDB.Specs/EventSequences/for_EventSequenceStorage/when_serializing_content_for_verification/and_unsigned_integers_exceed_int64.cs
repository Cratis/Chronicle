// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_serializing_content_for_verification;

public class and_unsigned_integers_exceed_int64 : Specification
{
    EventSequenceStorage _sequence;
    ExpandoObject _content;
    JsonSchema _schema;
    JsonObject _json;
    JsonNode _result;
    JsonNode _stored;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"value":{"type":"integer","format":"uint64"},"values":{"type":"array","items":{"type":"integer","format":"uint64"}}}}
            """);
        _json = JsonNode.Parse("""{"value":18446744073709551615,"values":[9223372036854775808,18446744073709551615]}""")!.AsObject();
        _content = new ExpandoObject();
        var converter = Substitute.For<Json.IExpandoObjectConverter>();
        converter.ToJsonObject(_content, _schema).Returns(_json);
        _sequence = new("store", "tenant", "log", Substitute.For<IEventStoreNamespaceDatabase>(), Substitute.For<IEventConverter>(), Substitute.For<IEventTypesStorage>(), Substitute.For<IIdentityStorage>(), converter, new JsonSerializerOptions(), NullLogger<EventSequenceStorage>.Instance);

        // The append representation survives BSON wire encoding before the read path renders it as JSON.
        var bson = EventContentBson.FromJson(_json.ToJsonString());
        _stored = JsonNode.Parse(EventContentBson.ToJson(BsonSerializer.Deserialize<BsonDocument>(bson.ToBson())))!;
    }

    void Because() => _result = JsonNode.Parse(_sequence.SerializeContentForVerification(_content, _schema))!;

    [Fact] void should_match_the_persisted_representation() => JsonNode.DeepEquals(_result, _stored).ShouldBeTrue();
    [Fact] void should_preserve_the_maximum_unsigned_integer() => _result["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_unsigned_array_elements() => _result["values"]![0]!.GetValue<ulong>().ShouldEqual((ulong)long.MaxValue + 1);
}
