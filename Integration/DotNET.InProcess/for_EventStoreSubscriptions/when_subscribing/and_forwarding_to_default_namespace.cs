// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_forwarding_to_default_namespace.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_forwarding_to_default_namespace(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.multi_event_store_subscription_setup(fixture)
    {
        public EventSourceId SourceId { get; private set; } = "test-source";
        public Concepts.Events.EventSequenceNumber InboxTailSequenceNumber { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;

        async Task Establish()
        {
            // Create subscription from source to target
            await Subscribe("basic-subscription", SourceEventStoreName, TargetEventStoreName);
        }

        async Task Because()
        {
            // Append an event to the source event store
            var testEvent = new TestEvent("Hello from source");
            await AppendEvent(SourceEventStoreName, SourceId, testEvent);

            // Give the subscription system time to forward the event
            await Task.Delay(500);

            // Check the inbox in the target event store
            InboxTailSequenceNumber = await GetInboxTailSequenceNumber(
                SourceEventStoreName,
                TargetEventStoreName,
                DefaultNamespace);
        }
    }

    [Fact]
    void should_persist_subscription_configuration() =>
        Context.SourceId.ShouldNotBeNull();

    [Fact]
    void should_have_basic_subscription_created() =>
        Context.InboxTailSequenceNumber.ShouldNotBeNull();
}
