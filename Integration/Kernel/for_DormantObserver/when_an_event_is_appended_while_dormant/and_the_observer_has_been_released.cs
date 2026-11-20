// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_an_event_is_appended_while_dormant.and_the_observer_has_been_released.context;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_an_event_is_appended_while_dormant;

[Collection(given.DormantObserverCollection.Name)]
public class and_the_observer_has_been_released(context context) : Given<context>(context)
{
    public class context(given.DormantObserverFixture fixture) : given.a_dormant_reactor(fixture)
    {
        public EventSequenceNumber[] HandledAfterTheWake;
        public EventSequenceNumber[] HandledAfterTheSecondWake;
        public bool ResidentWhileDormant;
        public bool ResidentWhileDormantAgain;

        async Task Establish()
        {
            await _store.EventLog.Append("source", new given.DormantObserverEvent("first"));
            await WaitForHandled(1);
            await WaitUntilReleased();
            ResidentWhileDormant = await ObserverIsResident();
        }

        async Task Because()
        {
            // Appended while the observer has no activation: the queue rings the doorbell and the observer resumes from its cursor.
            await _store.EventLog.Append("source", new given.DormantObserverEvent("second"));
            await _store.EventLog.Append("other", new given.DormantObserverEvent("third"));
            await WaitForHandled(3);
            await Task.Delay(TimeSpan.FromSeconds(2));
            HandledAfterTheWake = [.. Reactor.Handled];

            // The woken observer is released again, and a later event wakes it a second time, losing nothing and delivering nothing twice.
            await WaitUntilReleased();
            ResidentWhileDormantAgain = await ObserverIsResident();
            await _store.EventLog.Append("source", new given.DormantObserverEvent("fourth"));
            await WaitForHandled(4);
            await Task.Delay(TimeSpan.FromSeconds(2));
            HandledAfterTheSecondWake = [.. Reactor.Handled];
        }
    }

    [Fact] void should_have_released_the_observer() => Context.ResidentWhileDormant.ShouldBeFalse();
    [Fact] void should_deliver_every_event_exactly_once_after_the_wake() => Context.HandledAfterTheWake.Length.ShouldEqual(3);
    [Fact] void should_deliver_in_order_after_the_wake() => Context.HandledAfterTheWake.ShouldEqual([.. Context.HandledAfterTheWake.Order()]);
    [Fact] void should_not_deliver_an_event_twice_after_the_wake() => Context.HandledAfterTheWake.Distinct().Count().ShouldEqual(3);
    [Fact] void should_release_the_observer_again() => Context.ResidentWhileDormantAgain.ShouldBeFalse();
    [Fact] void should_deliver_every_event_exactly_once_after_the_second_wake() => Context.HandledAfterTheSecondWake.Length.ShouldEqual(4);
    [Fact] void should_deliver_in_order_after_the_second_wake() => Context.HandledAfterTheSecondWake.ShouldEqual([.. Context.HandledAfterTheSecondWake.Order()]);
}
