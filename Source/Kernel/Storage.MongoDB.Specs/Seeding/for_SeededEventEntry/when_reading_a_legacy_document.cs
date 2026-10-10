// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Seeding;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Seeding.for_SeededEventEntry;

public class when_reading_a_legacy_document : Specification
{
    BsonDocument _document;
    SeededEventEntry _result;

    void Establish()
    {
        _document = new SeededEventEntry("source", "type", "{}", []).ToBsonDocument();
        var map = BsonClassMap.LookupClassMap(typeof(SeededEventEntry));
        _document.Remove(map.GetMemberMap(nameof(SeededEventEntry.EventSourceType)).ElementName);
        _document.Remove(map.GetMemberMap(nameof(SeededEventEntry.EventStreamType)).ElementName);
        _document.Remove(map.GetMemberMap(nameof(SeededEventEntry.EventStreamId)).ElementName);
    }

    void Because() => _result = BsonSerializer.Deserialize<SeededEventEntry>(_document);

    [Fact] void should_default_source_type() => _result.EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_default_stream_type() => _result.EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_default_stream_id() => _result.EventStreamId.Value.ShouldEqual(EventStreamId.Default);
}
