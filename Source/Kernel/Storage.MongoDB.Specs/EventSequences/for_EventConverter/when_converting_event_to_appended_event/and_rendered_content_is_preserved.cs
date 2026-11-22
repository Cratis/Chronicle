// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_rendered_content_is_preserved : given.an_event_converter
{
    AppendedEvent _ordinary;
    AppendedEvent _decimal;
    AppendedEvent _unsigned;
    AppendedEvent _date;
    AppendedEvent _revised;

    async Task Because()
    {
        _ordinary = await _converter.ToAppendedEvent(WithContent(BsonDocument.Parse("""{"value":42,"nested":[{"name":"test"}]}""")));
        _decimal = await _converter.ToAppendedEvent(WithContent(new BsonDocument("nested", new BsonArray { new BsonDocument("value", new BsonDecimal128(123.4567890123456789012345678m)) })));
        _unsigned = await _converter.ToAppendedEvent(WithContent(new BsonDocument("value", new BsonDecimal128((decimal)ulong.MaxValue))));
        _date = await _converter.ToAppendedEvent(WithContent(new BsonDocument("value", new BsonDateTime(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)))));
        _revised = await _converter.ToAppendedEvent(WithContent(BsonDocument.Parse("""{"value":"original"}""")) with
        {
            Revisions = [new EventRevision(
                EventTypeGeneration.First,
                CorrelationId.NotSet,
                [],
                IdentityId.NotSet,
                DateTimeOffset.UnixEpoch,
                new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("""{"value":"revised"}""") },
                new Dictionary<string, string> { ["1"] = "revised-hash" })]
        });
    }

    [Fact] void should_preserve_ordinary_content_json() => JsonSerializer.Serialize(_ordinary.Content).ShouldEqual("""{"value":42,"nested":[{"name":"test"}]}""");
    [Fact] void should_preserve_nested_decimal_content_json() => JsonSerializer.Serialize(_decimal.Content).ShouldEqual("""{"nested":[{"value":123.4567890123456789012345678}]}""");
    [Fact] void should_preserve_unsigned_content_json() => JsonSerializer.Serialize(_unsigned.Content).ShouldEqual("""{"value":18446744073709551615}""");
    [Fact] void should_preserve_date_content_json() => JsonSerializer.Serialize(_date.Content).ShouldEqual("""{"value":{"$date":"2025-01-02T03:04:05Z"}}""");
    [Fact] void should_preserve_revision_content_json() => JsonSerializer.Serialize(_revised.Content).ShouldEqual("""{"value":"revised"}""");
    [Fact] void should_keep_the_original_generation_separate_from_the_revision() => _revised.GenerationalContent[1].ShouldEqual("{ \"value\" : \"original\" }");
    [Fact] void should_preserve_the_revision_hash() => _revised.Context.Hash.ShouldEqual(new EventHash("revised-hash"));

    static Event WithContent(BsonDocument content)
    {
        var @event = CreateEvent();
        @event.Content["1"] = content;
        return @event;
    }
}
