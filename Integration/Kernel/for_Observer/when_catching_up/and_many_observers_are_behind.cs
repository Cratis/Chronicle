// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_Observer.when_catching_up.and_many_observers_are_behind.context;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.when_catching_up;

/// <summary>
/// When a client application returns, every observer it owns is behind at once. No more than the configured number of catch-up
/// jobs run at the same time, and every observer still catches up in the end.
/// </summary>
/// <param name="context">The <see cref="context"/>.</param>
[Collection(given.CatchUpCapCollection.Name)]
public class and_many_observers_are_behind(context context) : Given<context>(context)
{
    public class context(given.CatchUpCapFixture fixture) : given.a_namespace_with_observers_behind_a_cap(fixture)
    {
        public int MostLiveCatchUpJobs;
        public int ReactorsThatCaughtUp;
        public int ObserversListed;

        async Task Because()
        {
            await StartOwner(waitUntilActive: true);
            await _store.EventLog.Append("first", new given.QuarantineRecoveryEvent("first"));
            await WaitUntil(() => Task.FromResult(Gate.Deliveries.Count >= 4), TimeSpan.FromSeconds(60));

            StopOwner();
            await WaitUntil(async () => (await GetObservers()).All(observer => observer.RunningState == Contracts.Observation.ObserverRunningState.Disconnected), TimeSpan.FromSeconds(60));
            ObserversListed = (await GetObservers()).Count;

            await _store.EventLog.Append("second", new given.QuarantineRecoveryEvent("second"));
            Gate.Close();
            await StartOwner(waitUntilActive: false);

            // While the gate is closed every catch-up job stays running; sample how many there are at once.
            var until = DateTimeOffset.UtcNow.AddSeconds(30);
            while (DateTimeOffset.UtcNow < until)
            {
                MostLiveCatchUpJobs = Math.Max(MostLiveCatchUpJobs, await CountLiveCatchUpJobs());

                // Task.Delay: sampling interval while observing the most catch-up jobs alive at once over a fixed window.
                await Task.Delay(500);
            }

            Gate.Open();
            await WaitUntil(
                () => Task.FromResult(Gate.Deliveries.Where(delivery => delivery.Content == "second").Select(delivery => delivery.Reactor).Distinct().Count() >= 4),
                TimeSpan.FromSeconds(120));
            ReactorsThatCaughtUp = Gate.Deliveries.Where(delivery => delivery.Content == "second").Select(delivery => delivery.Reactor).Distinct().Count();
        }
    }

    [Fact] void should_have_all_four_observers() => Context.ObserversListed.ShouldEqual(4);
    [Fact] void should_run_catch_up_jobs_during_the_burst() => Context.MostLiveCatchUpJobs.ShouldBeGreaterThan(0);
    [Fact] void should_not_run_more_catch_up_jobs_than_the_cap() => Context.MostLiveCatchUpJobs.ShouldBeLessThanOrEqual(given.a_namespace_with_observers_behind_a_cap.Cap);
    [Fact] void should_catch_up_every_observer_in_the_end() => Context.ReactorsThatCaughtUp.ShouldEqual(4);
}
