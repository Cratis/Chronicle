// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage;

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
        public int ResidentWhileEveryObserverIsDormant;
        public int[] HandledByTheOthersWhileOneIsWoken;
        public string[] ResidentRightAfterTheBetaEvent;
        public int BetaHandled;
        public int ResidentAfterTheBetaObserverWentDormantAgain;
        public bool AlphaRecordedBeforeTheDisconnect;
        public bool AlphaRecordClearedAfterTheDisconnect;
        public bool ListingWokeAnObserver;

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

            // Task.Delay: time-behavior; observe a quiet window to detect duplicate deliveries after waking.
            await Task.Delay(TimeSpan.FromSeconds(2));
            HandledAfterTheWake = [.. Reactor.Handled];

            // The woken observer is released again, and a later event wakes it a second time, losing nothing and delivering nothing twice.
            await WaitUntilReleased();
            ResidentWhileDormantAgain = await ObserverIsResident();
            await _store.EventLog.Append("source", new given.DormantObserverEvent("fourth"));
            await WaitForHandled(4);

            // Task.Delay: time-behavior; observe a quiet window to detect duplicates after the second wake.
            await Task.Delay(TimeSpan.FromSeconds(2));
            HandledAfterTheSecondWake = [.. Reactor.Handled];

            // Several observers dormant together: an event for one wakes that one and only that one, which handles it once and goes dormant again.
            IReactorHandler[] family = [_handler, _store.Reactors.GetHandlerFor<given.DormantAlphaReactor>(), _store.Reactors.GetHandlerFor<given.DormantBetaReactor>(), _store.Reactors.GetHandlerFor<given.DormantGammaReactor>()];
            var alpha = family[1];
            var beta = family[2];
            foreach (var handler in family)
            {
                await WaitUntilReleased(handler);
            }

            // Released one after the other can still leave the first one woken by now; wait for the moment none of them has an activation.
            await WaitUntilNoneIsResident(family);

            ResidentWhileEveryObserverIsDormant = (await Task.WhenAll(family.Select(async handler => (await ResidentObserverGrains(handler)).Count()))).Sum();

            // Listing and reading observers is the surface an operator uses; it must leave a dormant observer dormant.
            var residentBeforeListing = (await Task.WhenAll(family.Select(async handler => (await ResidentObserverGrains(handler)).Count()))).Sum();
            var information = await _store.Observers.GetAll();
            information.Any(observer => observer.Id == alpha.Id.Value).ShouldBeTrue();
            var residentAfterListing = (await Task.WhenAll(family.Select(async handler => (await ResidentObserverGrains(handler)).Count()))).Sum();
            ListingWokeAnObserver = residentAfterListing != residentBeforeListing;

            var betaSequence = await _store.EventLog.Append("source", new given.DormantBetaEvent("beta"));
            await WaitForHandled(ChronicleFixture.Beta.Handled, 1);
            ResidentRightAfterTheBetaEvent = [.. (await Task.WhenAll(family.Select(async handler => (await ResidentObserverGrains(handler)).Any() ? handler.Id.Value : string.Empty))).Where(id => id.Length > 0)];

            // Task.Delay: time-behavior; allow unwanted deliveries to show up before asserting other observers stayed idle.
            await Task.Delay(TimeSpan.FromSeconds(2));
            BetaHandled = ChronicleFixture.Beta.Handled.Count;
            HandledByTheOthersWhileOneIsWoken = [ChronicleFixture.Alpha.Handled.Count, ChronicleFixture.Gamma.Handled.Count, Reactor.Handled.Count];
            await WaitUntilReleased(beta);
            ResidentAfterTheBetaObserverWentDormantAgain = (await Task.WhenAll(family.Select(async handler => (await ResidentObserverGrains(handler)).Count()))).Sum();

            // A client that disconnects while its observers are dormant is still unsubscribed, and its durable record cleared.
            var states = Services.GetRequiredService<IStorage>().GetEventStore(_store.Name.Value).GetNamespace(Concepts.EventStoreNamespaceName.Default).Observers;
            var record = (await states.GetDurableSubscriptions(EventSequenceId.Log)).GetValueOrDefault(alpha.Id.Value);
            AlphaRecordedBeforeTheDisconnect = record is { Targets.Count: > 0 };
            if (record is { Targets.Count: > 0 })
            {
                var target = record.Targets[0];
                var grains = Services.GetRequiredService<IGrainFactory>();
                await grains.GetGrain<Clients.IConnectedClients>(target.Silo).OnClientDisconnected(new Concepts.Clients.ConnectionId(target.ConnectionId), "the client went away while the observer was dormant");
                await ReadinessWait.Until(
                    async () => (await states.GetDurableSubscriptions(EventSequenceId.Log)).ContainsKey(alpha.Id.Value),
                    recorded => !recorded,
                    TimeSpan.FromSeconds(60));
                AlphaRecordClearedAfterTheDisconnect = true;
            }
        }
    }

    [Fact] void should_have_released_the_observer() => Context.ResidentWhileDormant.ShouldBeFalse();
    [Fact] void should_deliver_every_event_exactly_once_after_the_wake() => Context.HandledAfterTheWake.Length.ShouldEqual(3);
    [Fact] void should_deliver_in_order_after_the_wake() => Context.HandledAfterTheWake.ShouldEqual([.. Context.HandledAfterTheWake.Order()]);
    [Fact] void should_not_deliver_an_event_twice_after_the_wake() => Context.HandledAfterTheWake.Distinct().Count().ShouldEqual(3);
    [Fact] void should_release_the_observer_again() => Context.ResidentWhileDormantAgain.ShouldBeFalse();
    [Fact] void should_deliver_every_event_exactly_once_after_the_second_wake() => Context.HandledAfterTheSecondWake.Length.ShouldEqual(4);
    [Fact] void should_deliver_in_order_after_the_second_wake() => Context.HandledAfterTheSecondWake.ShouldEqual([.. Context.HandledAfterTheSecondWake.Order()]);
    [Fact] void should_have_no_activation_for_any_observer_while_all_are_dormant() => Context.ResidentWhileEveryObserverIsDormant.ShouldEqual(0);
    [Fact] void should_not_wake_an_observer_by_listing_observers() => Context.ListingWokeAnObserver.ShouldBeFalse();
    [Fact] void should_wake_only_the_observer_the_event_is_for() => Context.ResidentRightAfterTheBetaEvent.Length.ShouldBeLessThanOrEqual(1);
    [Fact] void should_handle_the_event_exactly_once() => Context.BetaHandled.ShouldEqual(1);
    [Fact] void should_leave_the_other_reactors_untouched() => Context.HandledByTheOthersWhileOneIsWoken.ShouldEqual([0, 0, 4]);
    [Fact] void should_let_the_woken_observer_go_dormant_again() => Context.ResidentAfterTheBetaObserverWentDormantAgain.ShouldEqual(0);
    [Fact] void should_have_recorded_the_client_of_the_dormant_observer() => Context.AlphaRecordedBeforeTheDisconnect.ShouldBeTrue();
    [Fact] void should_clear_the_record_of_an_observer_whose_client_disconnected_while_it_was_dormant() => Context.AlphaRecordClearedAfterTheDisconnect.ShouldBeTrue();
}
