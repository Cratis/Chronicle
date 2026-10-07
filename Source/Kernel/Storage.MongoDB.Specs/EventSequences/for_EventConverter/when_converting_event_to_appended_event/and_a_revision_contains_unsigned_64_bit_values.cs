// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_a_revision_contains_unsigned_64_bit_values : given.an_event_converter
{
    Event _event;
    AppendedEvent _result;

    void Establish()
    {
        _event = CreateEvent();
        _event.Content["1"] = EventContentBson.FromJson("""{"value":18446744073709551614}""");
        _event = _event with
        {
            Revisions = [new EventRevision(
                1,
                CorrelationId.NotSet,
                [],
                IdentityId.NotSet,
                DateTimeOffset.UtcNow,
                new Dictionary<string, BsonDocument> { ["1"] = EventContentBson.FromJson("""{"value":18446744073709551615}""") },
                new Dictionary<string, string>())]
        };
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_read_the_revised_value_without_a_schema() => ((IDictionary<string, object?>)_result.Content)["value"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_expose_the_original_as_plain_json() => JsonNode.Parse(_result.OriginalContent)!["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue - 1);
    [Fact] void should_expose_revision_content_as_plain_json() => JsonNode.Parse(_result.Revisions.Single().Content)!["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
}
