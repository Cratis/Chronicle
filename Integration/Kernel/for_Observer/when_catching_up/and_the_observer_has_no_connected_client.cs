// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;

using context = Cratis.Chronicle.Kernel.Integration.for_Observer.when_catching_up.and_the_observer_has_no_connected_client.context;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.when_catching_up;

/// <summary>
/// An observer owned by a client application that is gone only fails when it is given work, so no catch-up job is started for it.
/// </summary>
/// <param name="context">The <see cref="context"/>.</param>
[Collection(given.OperatorCommandCollection.Name)]
public class and_the_observer_has_no_connected_client(context context) : Given<context>(context)
{
    public class context(given.OperatorCommandFixture fixture) : given.a_reactor_whose_client_is_gone(fixture)
    {
        public IEnumerable<Job> CatchUpJobs;
        public int Handled;

        async Task Because()
        {
            await _store.EventLog.Append("first", new given.QuarantineRecoveryEvent("first"));
            await WaitUntil(() => !Reactor.HandledSequenceNumbers.IsEmpty, TimeSpan.FromSeconds(30));

            DisposeOwner();
            await WaitForTheObserverToBeDisconnected(TimeSpan.FromSeconds(30));

            await _store.EventLog.Append("second", new given.QuarantineRecoveryEvent("second"));

            // Long enough for the watchdog and the dormant-observer sweeps to have looked at the observer that is behind.
            await Task.Delay(TimeSpan.FromSeconds(30));
            CatchUpJobs = await GetCatchUpJobs();
            Handled = Reactor.HandledSequenceNumbers.Count;
        }

        static async Task WaitUntil(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (!condition())
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    throw new TimeoutException("The condition was not met in time.");
                }

                await Task.Delay(250);
            }
        }
    }

    [Fact] void should_not_start_a_catch_up_job() => Context.CatchUpJobs.ShouldBeEmpty();
    [Fact] void should_not_deliver_the_event_it_is_behind_on() => Context.Handled.ShouldEqual(1);
}
