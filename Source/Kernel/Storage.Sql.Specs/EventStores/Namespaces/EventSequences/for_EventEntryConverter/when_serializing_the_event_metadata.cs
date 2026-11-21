// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

/// <summary>
/// Pins the exact stored JSON of causation, caused-by and tags, including escaping, so serialization can change without changing the stored form.
/// </summary>
public class when_serializing_the_event_metadata : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.FromHours(1));
    Causation[] _causation;
    IdentityId[] _causedBy;
    Tag[] _tags;
    string _causationJson;
    string _emptyCausationJson;
    string _causedByJson;
    string _emptyCausedByJson;
    string _tagsJson;
    string _noTagsJson;
    string _tagToken;
    EventEntry _entry;

    void Establish()
    {
        _causation =
        [
            new(_occurred, "first", new Dictionary<string, string> { ["Key"] = "v\"1", ["other"] = "é<>&" }),
            new(_occurred.AddMinutes(1), "second", new Dictionary<string, string>())
        ];
        _causedBy = [new(Guid.Parse("11111111-1111-1111-1111-111111111111")), new(Guid.Parse("22222222-2222-2222-2222-222222222222"))];
        _tags = [new("plain"), new("quo\"te é<>&")];
    }

    void Because()
    {
        _causationJson = EventEntryConverter.SerializeCausation(_causation);
        _emptyCausationJson = EventEntryConverter.SerializeCausation([]);
        _causedByJson = EventEntryConverter.SerializeCausedBy(_causedBy);
        _emptyCausedByJson = EventEntryConverter.SerializeCausedBy([]);
        _tagsJson = EventEntryConverter.SerializeTags(_tags);
        _noTagsJson = EventEntryConverter.SerializeTags([]);
        _tagToken = EventEntryConverter.SerializeTagToken(_tags[1]);
        _entry = EventEntryConverter.ToEventEntry(
            1,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            new EventType("event", 1),
            CorrelationId.New(),
            _causation,
            _causedBy,
            _tags,
            _occurred,
            new ExpandoObject());
    }

    [Fact] void should_pin_the_causation_json() => _causationJson.ShouldEqual("""[{"occurred":"2026-01-02T03:04:05.678+01:00","type":{"name":"first","value":"first"},"properties":{"Key":"v\u00221","other":"\u00E9\u003C\u003E\u0026"}},{"occurred":"2026-01-02T03:05:05.678+01:00","type":{"name":"second","value":"second"},"properties":{}}]""");
    [Fact] void should_serialize_no_causation_as_an_empty_array() => _emptyCausationJson.ShouldEqual("[]");
    [Fact] void should_pin_the_caused_by_json() => _causedByJson.ShouldEqual("""["11111111-1111-1111-1111-111111111111","22222222-2222-2222-2222-222222222222"]""");
    [Fact] void should_serialize_no_caused_by_as_an_empty_array() => _emptyCausedByJson.ShouldEqual("[]");
    [Fact] void should_pin_the_tags_json() => _tagsJson.ShouldEqual("""["plain","quo\u0022te \u00E9\u003C\u003E\u0026"]""");
    [Fact] void should_serialize_no_tags_as_an_empty_string() => _noTagsJson.ShouldEqual(string.Empty);
    [Fact] void should_pin_the_tag_token() => _tagToken.ShouldEqual("\"quo\\u0022te \\u00E9\\u003C\\u003E\\u0026\"");
    [Fact] void should_store_the_same_causation_on_the_entry() => _entry.Causation.ShouldEqual(_causationJson);
    [Fact] void should_store_the_same_caused_by_on_the_entry() => _entry.CausedBy.ShouldEqual(_causedByJson);
    [Fact] void should_store_the_same_tags_on_the_entry() => _entry.Tags.ShouldEqual(_tagsJson);
}
