// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_the_removal_is_logged_and_counted.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// Every removal is logged at information level with the observer, event store and namespace, and counted by the
/// unseen removal metric, once per observer.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_removal_is_logged_and_counted(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public long CountedBefore;
        public long CountedAfter;
        public string[] Logged = [];

        async Task Because()
        {
            CountedBefore = CountedRemovals;
            var alpha = new given.UnseenAlphaReactor();
            var client = await StartClient(alpha);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            await GoAway(client, ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);

            await Sweeper.Sweep();
            CountedAfter = CountedRemovals;
            Logged = LoggedAt(LogLevel.Information);
        }
    }

    [Fact] void should_count_one_removal() => (Context.CountedAfter - Context.CountedBefore).ShouldEqual(1);
    [Fact] void should_log_one_removal() => Context.Logged.Length.ShouldEqual(1);
    [Fact] void should_name_the_observer_in_the_log() => Context.Logged[0].ShouldContain(Context.ObserverId);
    [Fact] void should_name_the_event_store_in_the_log() => Context.Logged[0].ShouldContain(Context.StoreName);
    [Fact] void should_name_the_namespace_in_the_log() => Context.Logged[0].ShouldContain("Default");
}
