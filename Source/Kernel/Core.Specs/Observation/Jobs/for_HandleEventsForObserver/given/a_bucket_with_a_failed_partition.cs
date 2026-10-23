// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_bucket_with_a_failed_partition : a_performing_job_step
{
    protected Catch<JobStepResult> _firstAttempt;
    protected HandleEventsForObserverState _persistedState;

    async Task Establish()
    {
        _performState.Bucket = new(0, 1, 1);
        _persistedState = ReplaySerialization.Restore(_performState);
        _stateStorage.WriteStateAsync().Returns(_ =>
        {
            _persistedState = ReplaySerialization.Restore(_stateStorage.State);
            return Task.CompletedTask;
        });
        _eventCursor.Current.Returns([CreateEvent(1UL, "feature"), CreateEvent(2UL, "module")]);
        _eventCursor.MoveNext().Returns(Task.FromResult(true), Task.FromException<bool>(new InvalidOperationException("cursor interrupted")));
        _observerSubscriber.OnNext((Key)"feature", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "bad feature"));
        _firstAttempt = await _jobStep.InvokePerformStep(_performState);
        _jobStep.Run!.Close();
        _eventCursor.Current.Returns([CreateEvent(3UL, "feature"), CreateEvent(4UL, "module")]);
        _eventCursor.MoveNext().Returns(Task.FromResult(true), Task.FromResult(false));
    }
}
