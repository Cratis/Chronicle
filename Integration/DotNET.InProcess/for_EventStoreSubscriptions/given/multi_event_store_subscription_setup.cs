// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.given;

public class multi_event_store_subscription_setup(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
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

    /// <summary>
    /// Appends an event to a specific event store's event log.
    /// </summary>
    protected async Task AppendEvent(string eventStoreName, EventSourceId eventSourceId, object @event)
    {
        var eventStore = await ChronicleClient.GetEventStore(eventStoreName);
        await eventStore.EventLog.Append(eventSourceId, @event);
    }

    /// <summary>
    /// Appends an event to a specific namespace in a specific event store's event log.
    /// </summary>
    protected async Task AppendEvent(string eventStoreName, Concepts.EventStoreNamespaceName @namespace, EventSourceId eventSourceId, object @event)
    {
        var eventStore = await ChronicleClient.GetEventStore(eventStoreName, @namespace.Value);
        await eventStore.EventLog.Append(eventSourceId, @event);
    }

    /// <summary>
    /// Creates a subscription from a source event store to a target event store.
    /// </summary>
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
            TimeSpanFactory.FromSeconds(60));

        var targetStorage = Services.GetRequiredService<IStorage>().GetEventStore(targetEventStoreName);
        var targetSystemSequence = targetStorage
            .GetNamespace(DefaultNamespace)
            .GetEventSequence(Concepts.EventSequences.EventSequenceId.System);
        var tailSequenceNumber = (await targetSystemSequence.GetTailSequenceNumber()).Value;
        await subscriptionsReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);
    }

    /// <summary>
    /// Gets the tail sequence number of the inbox for a specific source in a target event store.
    /// </summary>
    protected async Task<Concepts.Events.EventSequenceNumber> GetInboxTailSequenceNumber(
        string sourceEventStoreName,
        string targetEventStoreName,
        Concepts.EventStoreNamespaceName targetNamespace)
    {
        var targetStorage = Services.GetRequiredService<IStorage>().GetEventStore(targetEventStoreName);
        var inboxSequenceId = new Concepts.EventSequences.EventSequenceId($"inbox-{sourceEventStoreName}");
        var inboxSequence = targetStorage
            .GetNamespace(targetNamespace)
            .GetEventSequence(inboxSequenceId);

        return await inboxSequence.GetTailSequenceNumber();
    }

    /// <summary>
    /// Waits for inbox tail sequence number changes caused by forwarding.
    /// </summary>
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

    /// <summary>
    /// Gets the count of events in the event log of a specific event store.
    /// </summary>
    protected async Task<int> GetEventLogCount(string eventStoreName)
    {
        var storage = Services.GetRequiredService<IStorage>().GetEventStore(eventStoreName);
        var eventLogSequence = storage
            .GetNamespace(DefaultNamespace)
            .GetEventSequence(Concepts.EventSequences.EventSequenceId.Log);

        var tail = await eventLogSequence.GetTailSequenceNumber();
        // If tail is max value, no events; otherwise count is tail + 1
        return tail.Equals(Concepts.Events.EventSequenceNumber.Unavailable) ? 0 : (int)(ulong)tail + 1;
    }

    /// <summary>
    /// Verifies that all subscriptions are stored in the target event store.
    /// </summary>
    protected async Task<IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>> GetStoredSubscriptions(string targetEventStoreName)
    {
        var storage = Services.GetRequiredService<IStorage>().GetEventStore(targetEventStoreName);
        return await storage.EventStoreSubscriptions.GetAll();
    }
}
