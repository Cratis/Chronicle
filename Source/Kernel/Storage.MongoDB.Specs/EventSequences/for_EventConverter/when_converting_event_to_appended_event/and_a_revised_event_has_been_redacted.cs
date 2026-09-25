// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_a_revised_event_has_been_redacted : given.an_event_converter
{
    AppendedEvent _converted;

    async Task Because()
    {
        var revised = CreateEvent() with
        {
            Revisions = [new EventRevision(
                EventTypeGeneration.First,
                CorrelationId.New(),
                [],
                IdentityId.NotSet,
                DateTimeOffset.UtcNow,
                new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("{\"secret\":\"revised-payload\"}") },
                new Dictionary<string, string> { ["1"] = "revised-hash" })]
        };
        var redacted = revised with
        {
            Type = GlobalEventTypes.Redaction,
            Content = new Dictionary<string, BsonDocument> { ["1"] = BsonDocument.Parse("{\"reason\":\"redacted\"}") },
            ContentHashes = new Dictionary<string, string>(),
            Revisions = []
        };
        _converted = await _converter.ToAppendedEvent(redacted);
    }

    [Fact] void should_read_the_redaction_payload() => ((IDictionary<string, object?>)_converted.Content)["reason"].ShouldEqual("redacted");
    [Fact] void should_not_expose_revisions() => _converted.Revisions.ShouldBeEmpty();
    [Fact] void should_return_no_content_hash() => _converted.Context.Hash.ShouldEqual(EventHash.NotSet);
}
