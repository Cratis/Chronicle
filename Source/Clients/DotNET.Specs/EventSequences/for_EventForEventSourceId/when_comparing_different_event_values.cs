// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.EventSequences.for_EventForEventSourceId;

public class when_comparing_different_event_values : Specification
{
    EventForEventSourceId _original;
    Dictionary<string, bool> _comparisons;

    void Establish() => _original = new("source", new object()) { Tags = ["tag"] };

    void Because()
    {
        _comparisons = new Dictionary<string, EventForEventSourceId>
        {
            [nameof(EventForEventSourceId.EventSourceId)] = _original with { EventSourceId = "other" },
            [nameof(EventForEventSourceId.Event)] = _original with { Event = new object() },
            [nameof(EventForEventSourceId.Causation)] = _original with { Causation = new(DateTimeOffset.UnixEpoch, CausationType.Unknown, new Dictionary<string, string>()) },
            [nameof(EventForEventSourceId.Subject)] = _original with { Subject = "person" },
            [nameof(EventForEventSourceId.EventStreamType)] = _original with { EventStreamType = "Payments" },
            [nameof(EventForEventSourceId.EventStreamId)] = _original with { EventStreamId = "stream" },
            [nameof(EventForEventSourceId.EventSourceType)] = _original with { EventSourceType = "Account" },
            [nameof(EventForEventSourceId.Occurred)] = _original with { Occurred = DateTimeOffset.UnixEpoch },
            [nameof(EventForEventSourceId.Tags)] = _original with { Tags = new List<string> { "tag" } }
        }.ToDictionary(_ => _.Key, _ => _original.Equals(_.Value));
    }

    [Fact] void should_compare_the_event_source_id() => _comparisons[nameof(EventForEventSourceId.EventSourceId)].ShouldBeFalse();
    [Fact] void should_compare_the_payload() => _comparisons[nameof(EventForEventSourceId.Event)].ShouldBeFalse();
    [Fact] void should_compare_the_causation() => _comparisons[nameof(EventForEventSourceId.Causation)].ShouldBeFalse();
    [Fact] void should_compare_the_subject() => _comparisons[nameof(EventForEventSourceId.Subject)].ShouldBeFalse();
    [Fact] void should_compare_the_stream_type() => _comparisons[nameof(EventForEventSourceId.EventStreamType)].ShouldBeFalse();
    [Fact] void should_compare_the_stream_id() => _comparisons[nameof(EventForEventSourceId.EventStreamId)].ShouldBeFalse();
    [Fact] void should_compare_the_source_type() => _comparisons[nameof(EventForEventSourceId.EventSourceType)].ShouldBeFalse();
    [Fact] void should_compare_the_occurrence_time() => _comparisons[nameof(EventForEventSourceId.Occurred)].ShouldBeFalse();
    [Fact] void should_preserve_tag_collection_reference_equality() => _comparisons[nameof(EventForEventSourceId.Tags)].ShouldBeFalse();
}
