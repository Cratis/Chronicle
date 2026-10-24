// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_replay_fence_is_raised_while_decrypting : given.a_performing_job_step_with_observer_filters
{
    void Establish()
    {
        _eventCursor.Current.Returns([CreateEvent(1UL, _audited)]);
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call =>
            {
                _observer.IsPartitionFenced(Arg.Any<Key>(), Arg.Any<JobId>()).Returns(true);
                return Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray());
            });
    }

    Task Because() => PerformStep();

    [Fact] void should_not_deliver_the_fenced_batch() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_finish_successfully() => _succeeded.ShouldBeTrue();
    [Fact] void should_not_advance_the_step_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
