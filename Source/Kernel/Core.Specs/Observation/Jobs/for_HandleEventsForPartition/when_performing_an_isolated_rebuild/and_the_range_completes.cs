// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing_an_isolated_rebuild;

public class and_the_range_completes : given.a_performing_isolated_rebuild
{
    void Establish() => _rebuilder.Publish(Arg.Any<EventSequenceNumber>()).Returns(true);

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_begin_the_rebuild_from_empty() => _rebuilder.Received(1).Begin(_performState.ObserverKey, _performState.Partition);
    [Fact] void should_publish_at_the_last_event_handled() => _rebuilder.Received(1).Publish(first_event_sequence_number);
    [Fact] void should_succeed() => StepResult.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_report_the_published_range_as_handled() => HandledResult()!.LastHandledEventSequenceNumber.ShouldEqual(first_event_sequence_number);
    [Fact] void should_not_checkpoint_progress() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_count_the_handled_events() => _observer.Received(1).ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(), Arg.Any<Cratis.Orleans.Jobs.JobStepId>(), first_event_sequence_number);
}
