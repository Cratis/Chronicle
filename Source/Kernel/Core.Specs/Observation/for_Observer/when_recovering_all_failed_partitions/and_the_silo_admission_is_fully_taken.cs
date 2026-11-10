// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

public class and_the_silo_admission_is_fully_taken : given.an_observer
{
    const int PartitionCount = 3;

    SubscriptionAdmission _admission;
    int _startedWhileTaken;
    int _startedAfterRelease;
    bool _admissionReturnedAfterBatch;

    void Establish()
    {
        _admission = new(1, Environment.ProcessorCount);
        _silo.AddService<ISubscriptionAdmission>(_admission);

        var failedPartitions = new FailedPartitions();
        for (var partition = 0; partition < PartitionCount; partition++)
        {
            failedPartitions.AddFailedPartition($"event-source-{partition}");
        }

        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = PartitionCount };
    }

    async Task Because()
    {
        await _admission.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        await _silo.TimerRegistry.FireAllAsync();
        _startedWhileTaken = RecoveryJobsStarted();

        _admission.Release();
        await _silo.TimerRegistry.FireAllAsync();
        _startedAfterRelease = RecoveryJobsStarted();

        _admissionReturnedAfterBatch = _admission.TryEnterForRecovery();
        if (_admissionReturnedAfterBatch) _admission.ReleaseRecovery();
    }

    [Fact] void should_not_recover_while_the_silo_admission_is_taken() => _startedWhileTaken.ShouldEqual(0);
    [Fact] void should_recover_once_admission_is_free() => _startedAfterRelease.ShouldEqual(PartitionCount);
    [Fact] void should_return_the_admission_after_the_batch() => _admissionReturnedAfterBatch.ShouldBeTrue();

    int RecoveryJobsStarted() => _jobsManager.ReceivedCalls()
        .Count(call => call.GetMethodInfo().Name == nameof(IJobsManager.Start) && call.GetArguments()[0] is RetryFailedPartitionRequest);
}
