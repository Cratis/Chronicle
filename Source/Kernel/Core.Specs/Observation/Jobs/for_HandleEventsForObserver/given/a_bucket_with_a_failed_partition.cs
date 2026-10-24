// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_bucket_with_a_failed_partition : a_performing_job_step
{
    protected Catch<JobStepResult> _firstAttempt;
    protected HandleEventsForObserverState _persistedState;
    SnapshottingHandleEventsForObserverStorage _snapshottingStorage;

    protected override Configuration.Jobs CreateJobsConfig() => new() { StepCheckpointBatchInterval = 1 };

    protected override IPersistentState<HandleEventsForObserverState> CreateStateStorage()
    {
        _snapshottingStorage = new();
        return _silo.AddPersistentStateStorage<HandleEventsForObserverState>(
            nameof(JobStepState),
            Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps,
            _snapshottingStorage);
    }

    async Task Establish()
    {
        _performState.Bucket = new(0, 1, 1);
        _eventCursor.Current.Returns([CreateEvent(1UL, "feature"), CreateEvent(2UL, "module")]);
        _eventCursor.MoveNext().Returns(Task.FromResult(true), Task.FromException<bool>(new InvalidOperationException("cursor interrupted")));
        _observerSubscriber.OnNext((Key)"feature", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "bad feature"));
        _firstAttempt = await _jobStep.InvokePerformStep(_performState);
        _persistedState = _snapshottingStorage.PersistedState;
        _jobStep.Run!.Close();
        _eventCursor.Current.Returns([CreateEvent(3UL, "feature"), CreateEvent(4UL, "module")]);
        _eventCursor.MoveNext().Returns(Task.FromResult(true), Task.FromResult(false));
    }
}
