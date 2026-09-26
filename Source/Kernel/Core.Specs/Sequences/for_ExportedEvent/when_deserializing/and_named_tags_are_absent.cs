// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Sequences.for_ExportedEvent.when_deserializing;

public class and_named_tags_are_absent : Specification
{
    ExportedEvent _event;

    void Because() => _event = JsonSerializer.Deserialize<ExportedEvent>("""
        {
          "SequenceNumber": 42,
          "EventType": "event-type",
          "EventSourceType": "source-type",
          "EventSourceId": "source-id",
          "EventStreamType": "stream-type",
          "CorrelationId": "00000000-0000-0000-0000-000000000001",
          "Occurred": "2025-01-01T00:00:00Z",
          "Tags": ["legacy"],
          "Content": "{}"
        }
        """)!;

    [Fact] void should_preserve_the_legacy_tags() => _event.Tags.ShouldContain("legacy");
    [Fact] void should_default_named_tags_to_empty() => _event.NamedTags.ShouldBeEmpty();
}
