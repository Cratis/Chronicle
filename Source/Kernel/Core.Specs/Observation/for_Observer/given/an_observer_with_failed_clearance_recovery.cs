// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_failed_clearance_recovery : an_observer_with_subscription
{
    protected readonly Exception _recoveryFailure = new InvalidOperationException("Recovery job query failed");
    protected readonly JobId _catchupJobId = JobId.New();
    protected readonly Key _retryablePartition = "retryable-partition";
    protected Exception? _clearanceError;
    protected bool _wasDisconnectedAfterFailure;
    protected bool _wasQuarantinedAfterFailure;
    protected bool _wasSubscribedAfterFailure;

    async Task Establish()
    {
        var failures = new FailedPartitions();
        failures.AddFailedPartition(_retryablePartition);
        _failedPartitionsStorage.State = failures;
        await _observer.TransitionTo<QuarantinedObserver>();

        var jobs = ImmutableList.Create(new JobState
        {
            Id = _catchupJobId,
            Status = JobStatus.Stopped,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.External, EventSequenceNumber.First, [EventType.Unknown])
        });
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromException<IImmutableList<JobState>>(_recoveryFailure), Task.FromResult<IImmutableList<JobState>>(jobs));
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();

        _clearanceError = await Catch.Exception(_observer.ClearObserverQuarantine);
        _wasDisconnectedAfterFailure = await _observer.GetCurrentState() is Disconnected;
        _wasQuarantinedAfterFailure = await _observer.IsObserverQuarantined();
        _wasSubscribedAfterFailure = await _observer.IsSubscribed();
    }
}
