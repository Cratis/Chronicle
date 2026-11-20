// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_removal_is_off.and_the_client_has_been_gone_longer_than_the_period.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_removal_is_off;

/// <summary>
/// The kernel's default: the period is short and the sweep runs every second, but nothing is removed because the removal was never
/// turned on. The time each client was last seen is still recorded.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.DisabledRemovalCollection.Name)]
public class and_the_client_has_been_gone_longer_than_the_period(context context) : Given<context>(context)
{
    public class context(given.DisabledRemovalFixture fixture) : given.an_unseen_observer_kernel<given.DisabledRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public int Handled;
        public DateTimeOffset GoneSince;
        public DateTimeOffset SweptAt;
        public int RemovedBySweep;
        public long CountedBefore;
        public given.Footprint Footprint = given.Footprint.None;

        async Task Because()
        {
            CountedBefore = CountedRemovals;
            var alpha = new given.UnseenAlphaReactor();
            var client = await StartClient(alpha);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            await _store.EventLog.Append("source", new given.UnseenEvent("first"));
            await _store.EventLog.Append("source", new given.UnseenEvent("second"));
            await WaitForHandled(alpha, 2);
            Handled = alpha.Handled.Count;

            await GoAway(client, ObserverId);
            GoneSince = await LastSeen(ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);

            SweptAt = Now;
            RemovedBySweep = await Sweeper.Sweep();
            Footprint = await FootprintOf(ObserverId);
        }
    }

    [Fact] void should_have_handled_the_events_while_the_client_was_there() => Context.Handled.ShouldEqual(2);
    [Fact] void should_have_recorded_when_the_client_was_last_seen_although_the_removal_is_off() => Context.GoneSince.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
    [Fact] void should_have_swept_long_after_the_period_ran_out() => (Context.SweptAt - Context.GoneSince).ShouldBeGreaterThan(given.UnseenObserverFixture.Period);
    [Fact] void should_remove_nothing_when_swept() => Context.RemovedBySweep.ShouldEqual(0);
    [Fact] void should_keep_the_definition() => Context.Footprint.HasDefinition.ShouldBeTrue();
    [Fact] void should_keep_the_state() => Context.Footprint.HasState.ShouldBeTrue();
    [Fact] void should_still_list_the_observer() => Context.Footprint.IsListed.ShouldBeTrue();
    [Fact] void should_not_count_a_removal() => Context.CountedRemovals.ShouldEqual(Context.CountedBefore);
    [Fact] void should_not_log_a_removal() => Context.LoggedAt(LogLevel.Information).ShouldBeEmpty();
}
