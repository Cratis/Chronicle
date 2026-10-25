// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.given;

public class multi_event_store_subscription_setup(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
{
    [EventType]
    public record TestEvent(string Value);

    [EventType]
    public record AnotherTestEvent(int Number);

    public string SourceEventStoreName { get; protected set; } = "source-event-store";
    public string TargetEventStoreName { get; protected set; } = "target-event-store";
    public string SecondSourceEventStoreName { get; protected set; } = "second-source-event-store";

    public Concepts.EventStoreNamespaceName DefaultNamespace { get; } = Concepts.EventStoreNamespaceName.Default;
    public Concepts.EventStoreNamespaceName TenantANamespace { get; } = "tenant-a";
    public Concepts.EventStoreNamespaceName TenantBNamespace { get; } = "tenant-b";

    public override IEnumerable<Type> EventTypes => [typeof(TestEvent), typeof(AnotherTestEvent)];

    protected async Task AppendEvent(string eventStoreName, EventSourceId eventSourceId, object @event)
    {
        var eventStore = await ChronicleClient.GetEventStore(eventStoreName);
        await eventStore.EventLog.Append(eventSourceId, @event);
    }

    protected async Task AppendEvent(string eventStoreName, Concepts.EventStoreNamespaceName @namespace, EventSourceId eventSourceId, object @event)
    {
        var eventStore = await ChronicleClient.GetEventStore(eventStoreName, @namespace.Value);
        await eventStore.EventLog.Append(eventSourceId, @event);
    }

    protected async Task Subscribe(
        string subscriptionId,
        string sourceEventStoreName,
        string targetEventStoreName,
        Action<IEventStoreSubscriptionBuilder>? configure = null)
    {
        var sourceEventStore = await ChronicleClient.GetEventStore(sourceEventStoreName);
        await sourceEventStore.RegisterAll();

        var targetEventStore = await ChronicleClient.GetEventStore(targetEventStoreName);
        await targetEventStore.RegisterAll();

        await targetEventStore.Subscriptions.Subscribe(
            new EventStoreSubscriptionId(subscriptionId),
            sourceEventStoreName,
            configure);

        // Wait for subscription reactor to process
        var subscriptionsReactor = await targetEventStore.Reactors.WaitForHandlerById(
            "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
            TimeSpanFactory.DefaultTimeout());

        var systemLog = targetEventStore.GetEventSequence(EventSequenceId.System);
        var tailSequenceNumber = (await systemLog.GetTailSequenceNumber()).Value;
        await subscriptionsReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);
    }

    protected async Task<Concepts.Events.EventSequenceNumber> GetInboxTailSequenceNumber(
        string sourceEventStoreName,
        string targetEventStoreName,
        Concepts.EventStoreNamespaceName targetNamespace)
    {
        var targetEventStore = await ChronicleClient.GetEventStore(targetEventStoreName, targetNamespace.Value);
        var inboxSequenceId = new EventSequenceId($"inbox-{sourceEventStoreName}");
        var inboxSequence = targetEventStore.GetEventSequence(inboxSequenceId);

        var clientTail = await inboxSequence.GetTailSequenceNumber();
        return new Concepts.Events.EventSequenceNumber(clientTail.Value);
    }

    protected async Task<Concepts.Events.EventSequenceNumber> WaitForInboxTailSequenceNumber(
        string sourceEventStoreName,
        string targetEventStoreName,
        Concepts.EventStoreNamespaceName targetNamespace,
        Concepts.Events.EventSequenceNumber expected,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        var current = Concepts.Events.EventSequenceNumber.Unavailable;

        while (DateTime.UtcNow < deadline)
        {
            current = await GetInboxTailSequenceNumber(sourceEventStoreName, targetEventStoreName, targetNamespace);

            if (current.Equals(expected))
            {
                return current;
            }

            await Task.Delay(100);
        }

        return current;
    }

    protected async Task<int> GetEventLogCount(string eventStoreName)
    {
        var eventStore = await ChronicleClient.GetEventStore(eventStoreName);
        var clientTail = await eventStore.EventLog.GetTailSequenceNumber();
        return clientTail == EventSequenceNumber.Unavailable ? 0 : (int)clientTail.Value + 1;
    }

    protected async Task<IEnumerable<EventStoreSubscriptionDefinition>> GetStoredSubscriptions(string targetEventStoreName)
    {
        var targetEventStore = await ChronicleClient.GetEventStore(targetEventStoreName);
        return await targetEventStore.Subscriptions.GetAll();
    }
}
