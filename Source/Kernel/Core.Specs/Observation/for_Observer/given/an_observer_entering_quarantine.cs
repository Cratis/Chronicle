// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_entering_quarantine : an_observer_with_subscription
{
    protected readonly TaskCompletionSource _cleanupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<IImmutableList<JobState>> _cleanupJobs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly JobId _catchupJobId = JobId.New();
    protected readonly JobId _retryJobId = JobId.New();
    protected readonly Key _recoveredPartition = "recovered-partition";
    protected readonly Key _retryablePartition = "retryable-partition";
    protected IImmutableList<JobState> _jobs;
    protected Task _quarantineEntry;

    async Task Establish()
    {
        var failures = new FailedPartitions();
        failures.AddFailedPartition(_retryablePartition);
        _failedPartitionsStorage.State = failures;
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { QuarantineOnFailedPartitionCount = 1 });
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>(), (EventSourceId)_recoveredPartition.ToString())
            .Returns(GetSequenceNumberError.StorageError);
        _jobs = ImmutableList.Create(
            new JobState
            {
                Id = _catchupJobId,
                Status = JobStatus.Stopped,
                Request = new CatchUpObserverRequest(_observerKey, ObserverType.External, EventSequenceNumber.First, [EventType.Unknown])
            },
            new JobState
            {
                Id = _retryJobId,
                Status = JobStatus.Stopped,
                Request = new RetryFailedPartitionRequest(_observerKey, ObserverType.External, _retryablePartition, EventSequenceNumber.First, [EventType.Unknown])
            });
        _jobsManager.GetAllJobs().Returns(_ => _cleanupEntered.TrySetResult() ? _cleanupJobs.Task : Task.FromResult(_jobs));
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();

        // FailedPartitionRecovered is AlwaysInterleave: clearance can run while its quarantine entry awaits cleanup.
        _quarantineEntry = _observer.FailedPartitionRecovered(_recoveredPartition, 42UL);
        await _cleanupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }
}
