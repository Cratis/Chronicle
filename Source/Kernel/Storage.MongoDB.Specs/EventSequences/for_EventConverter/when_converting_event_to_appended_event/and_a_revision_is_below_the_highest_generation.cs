// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_a_revision_is_below_the_highest_generation : given.an_event_converter
{
    AppendedEvent _converted;

    async Task Because()
    {
        var original = CreateEvent();
        var revised = original with
        {
            Content = new Dictionary<string, BsonDocument>
            {
                ["1"] = BsonDocument.Parse("{\"name\":\"original\"}"),
                ["2"] = BsonDocument.Parse("{\"name\":\"migrated\"}")
            },
            ContentHashes = new Dictionary<string, string> { ["1"] = "hash", ["2"] = "migrated-hash" },
            Revisions = [new EventRevision(
                EventTypeGeneration.First,
                CorrelationId.New(),
                [],
                IdentityId.NotSet,
                DateTimeOffset.UtcNow,
                new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("{\"name\":\"revised\"}") },
                new Dictionary<string, string> { ["1"] = "revised-hash" })]
        };
        _converted = await _converter.ToAppendedEvent(revised);
    }

    [Fact] void should_read_the_revised_content() => ((IDictionary<string, object?>)_converted.Content)["name"].ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation() => _converted.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_read_the_revised_hash() => _converted.Context.Hash.ShouldEqual(new EventHash("revised-hash"));
    [Fact] void should_be_revised() => _converted.IsRevised.ShouldBeTrue();
}
