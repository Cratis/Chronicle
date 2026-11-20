// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_waking_dormant_observers;

public class when_more_observers_wake_than_the_admission_allows : given.a_queue_with_a_stored_durable_subscription
{
    const int Observers = 6;
    const int Limit = 2;

    int _concurrent;
    int _maximum;
    int _woken;

    protected override IWakeAdmission CreateWakeAdmission() => new WakeAdmission(Limit, Environment.ProcessorCount);

    async Task Establish()
    {
        for (var index = 0; index < Observers; index++)
        {
            var id = IdInQueue(0, $"storm-{index}");
            Store(id, Storage.Observation.ObserverSubscriptionKind.ClientReactor);
            ObserverFor(KeyOf(id), async () =>
            {
                var now = Interlocked.Increment(ref _concurrent);
                int seen;
                while ((seen = Volatile.Read(ref _maximum)) < now && Interlocked.CompareExchange(ref _maximum, now, seen) != seen)
                {
                }

                await Task.Delay(30);
                Interlocked.Decrement(ref _concurrent);
                Interlocked.Increment(ref _woken);
            });
        }

        await _queue.SeedDormantSubscriptions();
    }

    async Task Because()
    {
        await _queue.Enqueue([EventAt(1)]);
        await _queue.AwaitQueueDepletion(periodNum: 500);
    }

    [Fact] void should_wake_every_observer() => _woken.ShouldEqual(Observers);
    [Fact] void should_never_wake_more_at_once_than_the_admission_allows() => _maximum.ShouldBeLessThanOrEqual(Limit);
    [Fact] void should_have_woken_more_than_one_at_a_time() => _maximum.ShouldBeGreaterThan(1);
    [Fact] void should_give_back_every_place() => _wakeAdmission.InProgress.ShouldEqual(0);
}
