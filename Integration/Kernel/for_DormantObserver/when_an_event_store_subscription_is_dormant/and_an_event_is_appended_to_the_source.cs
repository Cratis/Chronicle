// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_an_event_store_subscription_is_dormant.and_an_event_is_appended_to_the_source.context;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_an_event_store_subscription_is_dormant;

/// <summary>
/// The observer of an event store subscription holds no activation while the source is quiet, and an event appended to the
/// source outbox wakes it and is forwarded to the target inbox exactly once (#6117).
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.DormantObserverCollection.Name)]
public class and_an_event_is_appended_to_the_source(context context) : Given<context>(context)
{
    public class context(given.DormantObserverFixture fixture) : given.a_dormant_reactor<given.DormantObserverFixture>(fixture)
    {
        const string SubscriptionId = "dormant-subscription";

        string _sourceName = string.Empty;

        public override IEnumerable<Type> EventTypes => [.. base.EventTypes, typeof(given.DormantPublishedEvent)];

        public bool ResidentWhileDormant = true;
        public bool ResidentWhileDormantAgain = true;
        public ulong InboxTailAfterTheFirstEvent;
        public ulong InboxTailAfterTheWake;

        async Task Because()
        {
            var source = await ChronicleClient.GetEventStore($"dormant-source-{Guid.NewGuid():N}");
            _sourceName = source.Name.Value;
            await source.DiscoverAll();
            await source.RegisterAll();
            (await source.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

            await _store.Subscriptions.Subscribe(SubscriptionId, source.Name.Value, builder => builder.WithEventType<given.DormantPublishedEvent>());

            var outbox = source.GetEventSequence(EventSequenceId.Outbox);
            var inbox = _store.GetEventSequence(new EventSequenceId($"{EventSequenceId.InboxPrefix}{source.Name.Value}"));

            var first = await outbox.Append("source", new given.DormantPublishedEvent("first"));
            if (!first.IsSuccess)
            {
                throw new InvalidOperationException($"Appending to the source outbox failed: {string.Join("; ", first.Errors)}; {string.Join("; ", first.ConstraintViolations.Select(_ => _.Message))}");
            }

            await Retry(async () =>
            {
                var tail = await inbox.GetTailSequenceNumber();
                InboxTailAfterTheFirstEvent = tail == EventSequenceNumber.Unavailable ? 0 : tail.Value + 1;
                return InboxTailAfterTheFirstEvent >= 1;
            });

            await WaitUntilSubscriptionObserverIsReleased(source.Name.Value);
            ResidentWhileDormant = (await ResidentSubscriptionObservers(source.Name.Value)).Any();

            var expected = InboxTailAfterTheFirstEvent + 1;
            (await outbox.Append("source", new given.DormantPublishedEvent("second"))).IsSuccess.ShouldBeTrue();
            await Retry(async () =>
            {
                var tail = await inbox.GetTailSequenceNumber();
                InboxTailAfterTheWake = tail == EventSequenceNumber.Unavailable ? 0 : tail.Value + 1;
                return InboxTailAfterTheWake >= expected;
            });

            await WaitUntilSubscriptionObserverIsReleased(source.Name.Value);
            ResidentWhileDormantAgain = (await ResidentSubscriptionObservers(source.Name.Value)).Any();
        }

        async Task<IEnumerable<string>> ResidentSubscriptionObservers(string sourceEventStore) =>
            (await Services.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics())
                .Select(statistics => statistics.GrainId.ToString())
                .Where(grain =>
                    grain.StartsWith(ObserverGrainPrefix, StringComparison.OrdinalIgnoreCase) &&
                    grain.Contains($"{SubscriptionId}->{_store.Name.Value}", StringComparison.OrdinalIgnoreCase) &&
                    grain.Contains(sourceEventStore, StringComparison.Ordinal))
                .ToArray();

        async Task WaitUntilSubscriptionObserverIsReleased(string sourceEventStore)
        {
            // The alert reminder of a new observer is due one to two minutes after it subscribed; it is not dormant before that.
            var deadline = DateTimeOffset.UtcNow.AddSeconds(240);
            while ((await ResidentSubscriptionObservers(sourceEventStore)).Any())
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    throw new TimeoutException($"The subscription observer was not released: {string.Join(", ", await ResidentSubscriptionObservers(sourceEventStore))}");
                }

                await Task.Delay(500);
            }
        }

        async Task Retry(Func<Task<bool>> attempt)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
            while (!await attempt())
            {
                if (DateTimeOffset.UtcNow > deadline)
                {
                    var key = new Concepts.Observation.ObserverKey($"{SubscriptionId}->{_store.Name.Value}", _sourceName, Concepts.EventStoreNamespaceName.Default, Concepts.EventSequences.EventSequenceId.Outbox);
                    var observer = Services.GetRequiredService<IGrainFactory>().GetGrain<Cratis.Chronicle.Observation.IObserver>(key.ToString());
                    var state = await observer.GetState();
                    var definitions = string.Join(", ", (await _store.Subscriptions.GetAll()).Select(subscription => subscription.Id.Value));
                    throw new TimeoutException($"The event was not forwarded to the target in time: subscribed {await observer.IsSubscribed()}, running state {state.RunningState}, next {state.NextEventSequenceNumber}, last handled {state.LastHandledEventSequenceNumber}; subscriptions [{definitions}]");
                }

                await Task.Delay(250);
            }
        }
    }

    [Fact] void should_forward_the_first_event() => Context.InboxTailAfterTheFirstEvent.ShouldBeGreaterThanOrEqual(1UL);
    [Fact] void should_release_the_observer_while_the_source_is_quiet() => Context.ResidentWhileDormant.ShouldBeFalse();
    [Fact] void should_forward_the_event_appended_while_dormant_exactly_once() => Context.InboxTailAfterTheWake.ShouldEqual(Context.InboxTailAfterTheFirstEvent + 1);
    [Fact] void should_release_the_observer_again() => Context.ResidentWhileDormantAgain.ShouldBeFalse();
}
