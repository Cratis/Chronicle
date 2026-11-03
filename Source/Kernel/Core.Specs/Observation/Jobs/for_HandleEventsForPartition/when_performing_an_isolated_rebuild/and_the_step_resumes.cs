// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing_an_isolated_rebuild;

public class and_the_step_resumes : given.a_performing_isolated_rebuild
{
    void Establish()
    {
        _performState.LastSuccessfullyHandledEventSequenceNumber = 40UL;
        _rebuilder.Publish(Arg.Any<EventSequenceNumber>()).Returns(true);
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_begin_the_rebuild_again() => _rebuilder.Received(1).Begin(_performState.ObserverKey, _performState.Partition);

    [Fact]
    void should_read_from_the_start_of_the_range() =>
        _eventSequence.Received(1).GetRange(
            _performState.StartEventSequenceNumber,
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>());
}
