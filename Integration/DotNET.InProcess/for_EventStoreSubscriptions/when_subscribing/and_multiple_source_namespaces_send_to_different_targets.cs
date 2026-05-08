// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_multiple_source_namespaces_send_to_different_targets.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_multiple_source_namespaces_send_to_different_targets(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.multi_event_store_subscription_setup(fixture)
    {
        public Concepts.Events.EventSequenceNumber TenantAInboxTail { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;
        public Concepts.Events.EventSequenceNumber TenantBInboxTail { get; private set; } = Concepts.Events.EventSequenceNumber.Unavailable;

        async Task Establish()
        {
            // Single subscription from source to target
            // The subscription system automatically delivers to all namespaces
            await Subscribe("multi-namespace-subscription", SourceEventStoreName, TargetEventStoreName);
        }

        async Task Because()
        {
            // Append event to source outbox
            var testEvent = new TestEvent("Multi-namespace test");
            await AppendEvent(SourceEventStoreName, "multi-ns-source", testEvent);

            // Wait for forwarding
            await Task.Delay(500);

            // Verify inbox received event in both namespaces
            TenantAInboxTail = await GetInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, TenantANamespace);
            TenantBInboxTail = await GetInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, TenantBNamespace);
        }
    }
}
