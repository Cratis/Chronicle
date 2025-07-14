// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations.when_performing;

public class without_any_causation_set : given.event_sequence_operations_without_any_operations
{
    EventSourceId _eventSourceId;
    object _appendedEvent;
    AppendManyResult _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _appendedEvent = new object();
        _operations.ForEventSourceId(_eventSourceId, builder => builder.Append(_appendedEvent));
    }

    async Task Because() => _result = await _operations.Perform();

    [Fact] void should_call_append_many_on_event_sequence() => _eventSequence.Received().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>());
    [Fact] void should_return_append_many_result() => _result.ShouldNotBeNull();
}
