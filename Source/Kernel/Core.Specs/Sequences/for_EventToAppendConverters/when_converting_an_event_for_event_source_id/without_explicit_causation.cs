// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventToAppendConverters.when_converting_an_event_for_event_source_id;

public class without_explicit_causation : Specification
{
    EventForEventSourceId _result;

    void Because() => _result = new Contracts.Sequences.EventForEventSourceId
    {
        EventSourceId = "source",
        EventType = new() { Id = "event", Generation = 1 },
        Content = "{}"
    }.ToApi();

    [Fact] void should_leave_causation_unset() => _result.Causation.ShouldBeNull();
}
