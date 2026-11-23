// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_reporting_dormancy_metrics;

public class and_the_admission_is_full : given.a_queue_reporting_its_metrics
{
    const int Observers = 4;
    readonly Observation.WakeAdmission _admission = new(1, Environment.ProcessorCount);
    readonly TaskCompletionSource _wakeWaiting = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override Observation.IWakeAdmission CreateWakeAdmission()
    {
        var admission = Substitute.For<Observation.IWakeAdmission>();
        admission.Limit.Returns(_admission.Limit);
        admission.InProgress.Returns(_ => _admission.InProgress);
        admission.WaitAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            var waiting = _admission.WaitAsync(call.Arg<CancellationToken>());
            if (!waiting.IsCompleted) _wakeWaiting.TrySetResult();
            return waiting;
        });
        admission.When(_ => _.Release()).Do(_ => _admission.Release());
        return admission;
    }

    async Task Establish()
    {
        for (var index = 0; index < Observers; index++)
        {
            var id = IdInQueue(0, $"storm-{index}");
            Store(id, Storage.Observation.ObserverSubscriptionKind.ClientReactor);
            ObserverFor(KeyOf(id));
        }

        await _queue.SeedDormantSubscriptions();
    }

    async Task Because()
    {
        // Hold the real slot until a wake is waiting for it; observer/thread startup timing cannot create the overlap.
        await _admission.WaitAsync(CancellationToken.None);
        try
        {
            await _queue.Enqueue([EventAt(1)]);
            await _wakeWaiting.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _admission.Release();
        }
        await _queue.AwaitQueueDepletion(periodNum: 500);
    }

    [Fact] void should_count_the_wakes_that_had_to_wait() => Recorded(WellKnownMetricNames.ObserverDormantWakesDelayed).ShouldBeGreaterThan(0);
}
