// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_reporting_dormancy_metrics;

public class and_the_admission_is_full : given.a_queue_reporting_its_metrics
{
    const int Observers = 4;

    protected override Observation.IWakeAdmission CreateWakeAdmission() => new Observation.WakeAdmission(1, Environment.ProcessorCount);

    async Task Establish()
    {
        for (var index = 0; index < Observers; index++)
        {
            var id = IdInQueue(0, $"storm-{index}");
            Store(id, Storage.Observation.ObserverSubscriptionKind.ClientReactor);
            ObserverFor(KeyOf(id), () => Task.Delay(30));
        }

        await _queue.SeedDormantSubscriptions();
    }

    async Task Because()
    {
        await _queue.Enqueue([EventAt(1)]);
        await _queue.AwaitQueueDepletion(periodNum: 500);
    }

    [Fact] void should_count_the_wakes_that_had_to_wait() => Recorded(WellKnownMetricNames.ObserverDormantWakesDelayed).ShouldBeGreaterThan(0);
}
