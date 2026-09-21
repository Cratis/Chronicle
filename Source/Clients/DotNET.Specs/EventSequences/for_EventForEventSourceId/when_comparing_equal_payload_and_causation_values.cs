// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventForEventSourceId;

public class when_comparing_equal_payload_and_causation_values : Specification
{
    EventForEventSourceId _implicit;
    EventForEventSourceId _explicit;

    void Establish()
    {
        var properties = new Dictionary<string, string> { ["operation"] = "record" };
        string[] tags = ["tag"];
        _implicit = new("source", new RecordedValue("value"), new(DateTimeOffset.UnixEpoch, CausationType.Unknown, properties)) { Tags = tags };
        _explicit = new("source", new RecordedValue("value"), new(DateTimeOffset.UnixEpoch, CausationType.Unknown, properties))
        {
            EventSourceType = EventSourceType.Default,
            EventStreamType = EventStreamType.All,
            EventStreamId = EventStreamId.Default,
            Tags = tags
        };
    }

    [Fact] void should_preserve_payload_and_causation_value_equality() => _implicit.Equals(_explicit).ShouldBeTrue();
    [Fact] void should_preserve_matching_hash_codes() => _implicit.GetHashCode().ShouldEqual(_explicit.GetHashCode());

    record RecordedValue(string Value);
}
