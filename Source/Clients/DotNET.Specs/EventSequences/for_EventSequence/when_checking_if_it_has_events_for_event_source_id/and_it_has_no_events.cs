// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_checking_if_it_has_events_for_event_source_id;

public class and_it_has_no_events : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    bool _result;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _eventSequences.HasEventsForEventSourceId(Arg.Is<Contracts.EventSequences.HasEventsForEventSourceIdRequest>(
            req => req.EventSourceId == _eventSourceId)).Returns(Task.FromResult(new Contracts.EventSequences.HasEventsForEventSourceIdResponse { HasEvents = false }));
    }

    async Task Because() => _result = await _eventSequence.HasEventsFor(_eventSourceId);

    [Fact] void should_return_false_when_no_events_exist() => _result.ShouldBeFalse();
}
