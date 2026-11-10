// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

public class and_recoveries_hold_their_share_of_the_silo_admission : given.an_observer
{
    SubscriptionAdmission _admission;
    int _started;
    bool _subscriptionStillAdmitted;

    void Establish()
    {
        _admission = new(2, Environment.ProcessorCount);
        _silo.AddService<ISubscriptionAdmission>(_admission);

        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition("event-source");
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
    }

    async Task Because()
    {
        _admission.TryEnterForRecovery();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

        await _silo.TimerRegistry.FireAllAsync();
        _started = _jobsManager.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IJobsManager.Start) && call.GetArguments()[0] is RetryFailedPartitionRequest);

        _subscriptionStillAdmitted = await _admission.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        if (_subscriptionStillAdmitted) _admission.Release();
        _admission.ReleaseRecovery();
    }

    [Fact] void should_not_recover_beyond_the_recovery_share() => _started.ShouldEqual(0);
    [Fact] void should_leave_the_rest_for_subscriptions() => _subscriptionStillAdmitted.ShouldBeTrue();
}
