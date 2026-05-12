// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_same_namespace_subscribes_from_multiple_sources.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_same_namespace_subscribes_from_multiple_sources(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.multi_event_store_subscription_setup(fixture)
    {
        public Concepts.Events.EventSequenceNumber FirstSourceInboxTail { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;
        public Concepts.Events.EventSequenceNumber SecondSourceInboxTail { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;

        async Task Establish()
        {
            // Subscribe to events from first source
            await Subscribe("subscription-from-source-1", SourceEventStoreName, TargetEventStoreName);

            // Subscribe to events from second source
            await Subscribe("subscription-from-source-2", SecondSourceEventStoreName, TargetEventStoreName);
        }

        async Task Because()
        {
            // Append event to first source
            var event1 = new TestEvent("From source 1");
            await AppendEvent(SourceEventStoreName, "source-1-id", event1);

            // Append event to second source
            var event2 = new TestEvent("From source 2");
            await AppendEvent(SecondSourceEventStoreName, "source-2-id", event2);

            // Wait for forwarding
            await Task.Delay(500);

            // Verify both sources have their events in separate inboxes
            FirstSourceInboxTail = await GetInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, DefaultNamespace);
            SecondSourceInboxTail = await GetInboxTailSequenceNumber(SecondSourceEventStoreName, TargetEventStoreName, DefaultNamespace);
        }
    }

    [Fact]
    void should_subscribe_from_first_source() =>
        Context.FirstSourceInboxTail.ShouldNotBeNull();

    [Fact]
    void should_subscribe_from_second_source() =>
        Context.SecondSourceInboxTail.ShouldNotBeNull();
}
