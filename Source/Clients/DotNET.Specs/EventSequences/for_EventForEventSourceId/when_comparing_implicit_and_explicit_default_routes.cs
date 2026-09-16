// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventForEventSourceId;

public class when_comparing_implicit_and_explicit_default_routes : Specification
{
    EventForEventSourceId _implicit;
    EventForEventSourceId _explicit;

    void Establish()
    {
        var payload = new object();
        _implicit = new("source", payload);
        _explicit = new("source", payload)
        {
            EventSourceType = EventSourceType.Default,
            EventStreamType = EventStreamType.All,
            EventStreamId = EventStreamId.Default
        };
    }

    [Fact] void should_preserve_public_value_equality() => _implicit.Equals(_explicit).ShouldBeTrue();
    [Fact] void should_preserve_matching_hash_codes() => _implicit.GetHashCode().ShouldEqual(_explicit.GetHashCode());
}
