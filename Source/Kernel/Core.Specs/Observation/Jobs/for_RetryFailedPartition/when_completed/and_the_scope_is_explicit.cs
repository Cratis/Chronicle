// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// Whether a step that read nothing has anything left to read is asked of the scope the request carries.
/// </summary>
public class and_the_scope_is_explicit : given.a_retry_failed_partition_job
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    IEnumerable<EventType>? _lookedFor;

    void Establish()
    {
        _request = _request with { ReaderScope = ObserverReaderScope.Explicit([_eventType]) };
        _stateStorage.State.HandledAllEvents = true;
        _eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<EventSourceId?>())
            .Returns(EventSequenceNumber.Unavailable)
            .AndDoes(call => _lookedFor = call.Arg<IEnumerable<EventType>?>());
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_look_only_for_the_event_types_of_the_scope() => _lookedFor.ShouldEqual([_eventType]);
}
