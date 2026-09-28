// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Sequences.for_ExportedEvent.when_deserializing;

public class and_named_tags_are_present : Specification
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
          "Content": "{}",
          "NamedTags": [{"Name": "region", "Value": "north"}]
        }
        """)!;

    [Fact] void should_preserve_the_named_tag_name() => _event.NamedTags.Single().Name.ShouldEqual("region");
    [Fact] void should_preserve_the_named_tag_value() => _event.NamedTags.Single().Value.ShouldEqual("north");
}
