// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// Whether a step that read nothing has anything left to read is asked of the scope the request carries.
/// </summary>
public class and_the_scope_is_a_wildcard : given.a_retry_failed_partition_job
{
    IEnumerable<EventType>? _lookedFor;

    void Establish()
    {
        _request = _request with { ReaderScope = ObserverReaderScope.Wildcard };
        _stateStorage.State.HandledAllEvents = true;
        _eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<EventSourceId?>())
            .Returns(EventSequenceNumber.Unavailable)
            .AndDoes(call => _lookedFor = call.Arg<IEnumerable<EventType>?>());
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_look_for_every_event_type() => _lookedFor.ShouldBeNull();
}
