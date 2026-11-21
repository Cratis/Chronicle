// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_client_returns.and_comes_back_before_the_period_has_elapsed.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_the_client_returns;

/// <summary>
/// A client that reconnects just before the period elapses starts the clock again: sweeping at a moment when the first absence has
/// lasted longer than the period removes nothing, and the observer goes only after the period from the second absence.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_comes_back_before_the_period_has_elapsed(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public DateTimeOffset FirstGoneAt;
        public DateTimeOffset SecondGoneAt;
        public DateTimeOffset SweptAt;
        public int RemovedWhileTheFirstAbsenceWasOverTheLimit;
        public int RemovedAfterTheSecondAbsence;
        public given.Footprint AfterTheEarlySweep = given.Footprint.None;
        public given.Footprint AfterTheLateSweep = given.Footprint.None;

        async Task Because()
        {
            var first = new given.UnseenAlphaReactor();
            var client = await StartClient(first);
            ObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;
            await GoAway(client, ObserverId);
            FirstGoneAt = await LastSeen(ObserverId);

            await WaitUntil("most of the period to pass", () => Task.FromResult(DateTimeOffset.UtcNow > FirstGoneAt + (given.UnseenObserverFixture.Period * 0.75)));
            var back = await StartClient(new given.UnseenAlphaReactor());
            await WaitUntil("the first absence to be longer than the period", () => Task.FromResult(DateTimeOffset.UtcNow > FirstGoneAt + given.UnseenObserverFixture.Period + TimeSpan.FromMilliseconds(250)));
            await GoAway(back, ObserverId);
            SecondGoneAt = await LastSeen(ObserverId);

            SweptAt = DateTimeOffset.UtcNow;
            RemovedWhileTheFirstAbsenceWasOverTheLimit = await Sweeper.Sweep();
            AfterTheEarlySweep = await FootprintOf(ObserverId);

            await WaitUntilUnseenForThePeriod(ObserverId);
            RemovedAfterTheSecondAbsence = await Sweeper.Sweep();
            AfterTheLateSweep = await FootprintOf(ObserverId);
        }
    }

    [Fact] void should_have_swept_when_the_first_absence_was_longer_than_the_period() => (Context.SweptAt - Context.FirstGoneAt).ShouldBeGreaterThan(given.UnseenObserverFixture.Period);
    [Fact] void should_have_swept_before_the_second_absence_was_as_long_as_the_period() => (Context.SweptAt - Context.SecondGoneAt).ShouldBeLessThan(given.UnseenObserverFixture.Period);
    [Fact] void should_have_started_the_clock_again() => Context.SecondGoneAt.ShouldBeGreaterThan(Context.FirstGoneAt + (given.UnseenObserverFixture.Period / 2));
    [Fact] void should_remove_nothing_then() => Context.RemovedWhileTheFirstAbsenceWasOverTheLimit.ShouldEqual(0);
    [Fact] void should_keep_the_observer_then() => (Context.AfterTheEarlySweep.HasDefinition && Context.AfterTheEarlySweep.HasState).ShouldBeTrue();
    [Fact] void should_remove_it_after_the_period_from_the_second_absence() => Context.RemovedAfterTheSecondAbsence.ShouldEqual(1);
    [Fact] void should_leave_nothing_of_it() => Context.AfterTheLateSweep.ShouldEqual(given.Footprint.None);
}
