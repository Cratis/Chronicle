// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_filtering_event_types.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_filtering_event_types(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.multi_event_store_subscription_setup(fixture)
    {
        public int EventsInSourceLog { get; private set; }
        public Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition Subscription { get; private set; }

        async Task Establish()
        {
            // Subscribe with event type filter - only TestEvent, not AnotherTestEvent
            await Subscribe(
                "filtered-subscription",
                SourceEventStoreName,
                TargetEventStoreName,
                builder => builder.WithEventType<TestEvent>());
        }

        async Task Because()
        {
            // Append two TestEvent events
            var testEvent1 = new TestEvent("First event");
            await AppendEvent(SourceEventStoreName, "source-1", testEvent1);

            var testEvent2 = new TestEvent("Second event");
            await AppendEvent(SourceEventStoreName, "source-2", testEvent2);

            // Append an AnotherTestEvent that should NOT be forwarded
            var anotherEvent = new AnotherTestEvent(42);
            await AppendEvent(SourceEventStoreName, "source-3", anotherEvent);

            EventsInSourceLog = await GetEventLogCount(SourceEventStoreName);

            // Wait for forwarding
            await Task.Delay(500);

            // Get subscription configuration
            var subscriptions = await GetStoredSubscriptions(TargetEventStoreName);
            Subscription = subscriptions.Single(_ => _.Identifier.Value == "filtered-subscription");
        }
    }

    [Fact]
    void should_have_three_events_in_source_log() =>
        Context.EventsInSourceLog.ShouldEqual(3);

    [Fact]
    void should_configure_event_type_filter() =>
        Context.Subscription.EventTypes.Count().ShouldEqual(1);

    [Fact]
    void should_only_filter_test_event_type() =>
        Context.Subscription.EventTypes.Single().Id.Value.ShouldEqual("TestEvent");
}
