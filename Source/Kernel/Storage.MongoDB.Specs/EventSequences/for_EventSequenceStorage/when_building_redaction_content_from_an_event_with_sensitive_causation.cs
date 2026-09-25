// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

public class when_building_redaction_content_from_an_event_with_sensitive_causation : Specification
{
    const string Secret = "sensitive-command-property";
    Event _original;
    RedactionEventContent _content;

    void Establish()
    {
        _original = new Event(
            1,
            CorrelationId.New(),
            [new Causation(DateTimeOffset.UtcNow, "command", new Dictionary<string, string> { ["apiKey"] = Secret })],
            [IdentityId.NotSet],
            "d0b8f8a4-6d0d-4a1a-9a0a-1a2b3c4d5e6f",
            DateTimeOffset.UtcNow,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            [],
            new Dictionary<string, BsonDocument>(),
            new Dictionary<string, string>(),
            []);
    }

    void Because() => _content = EventSequenceStorage.CreateRedactionContent(_original, "contains sensitive data");

    [Fact] void should_erase_the_original_causation() => _content.Causation.ShouldBeEmpty();
    [Fact] void should_keep_the_original_event_type() => _content.OriginalEventType.ShouldEqual(_original.Type);
    [Fact] void should_keep_the_original_occurrence() => _content.Occurred.ShouldEqual(_original.Occurred);
    [Fact] void should_keep_the_original_correlation() => _content.CorrelationId.ShouldEqual(_original.CorrelationId);
    [Fact] void should_keep_the_original_caused_by_chain() => _content.CausedBy.ShouldEqual(_original.CausedBy);
    [Fact] void should_not_retain_the_sensitive_command_property() => System.Text.Json.JsonSerializer.Serialize(_content).ShouldNotContain(Secret);
}
