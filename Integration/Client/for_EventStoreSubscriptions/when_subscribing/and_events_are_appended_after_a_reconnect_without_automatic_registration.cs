// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_subscribing.and_events_are_appended_after_a_reconnect_without_automatic_registration.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_events_are_appended_after_a_reconnect_without_automatic_registration(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.multi_event_store_subscription_setup(fixture)
    {
#pragma warning disable CA2213 // Destroy disposes the client through the specification lifecycle.
        ChronicleClient _client;
#pragma warning restore CA2213
        public Concepts.Events.EventSequenceNumber ForwardedInboxTailSequenceNumber = Concepts.Events.EventSequenceNumber.Unavailable;

        public override IEnumerable<Type> EventTypes => [typeof(ReconnectForwardedEvent)];

        async Task Establish()
        {
            SourceEventStoreName = "reconnect-source-event-store";
            TargetEventStoreName = "reconnect-target-event-store";
            _client = new ChronicleClient(
                new BorrowedConnection(EventStore.Connection),
                new ChronicleOptions
                {
                    AutoDiscoverAndRegister = false,
                    DefaultSinkTypeId = Services.GetRequiredService<IChronicleClient>().Options.DefaultSinkTypeId
                },
                artifactsProvider: new ExplicitArtifacts(),
                serviceProvider: Services);

            var source = await ChronicleClient.GetEventStore(SourceEventStoreName);
            await source.RegisterAll();
            var target = await _client.GetEventStore(TargetEventStoreName);
            await target.DiscoverAll();
            await target.RegisterAll();
            await target.Subscriptions.Subscribe(new EventStoreSubscriptionId("reconnect-subscription"), SourceEventStoreName);
            await WaitForSubscriptionsToBeProcessed(TargetEventStoreName);
        }

        async Task Because()
        {
            await AppendToSourceOutbox("first", new ReconnectForwardedEvent("before"));
            var firstTail = await WaitForInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, DefaultNamespace, expected: 0, timeout: TimeSpan.FromSeconds(30));
            firstTail.Value.ShouldEqual(0UL);

            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();

            await AppendToSourceOutbox("second", new ReconnectForwardedEvent("after"));
            ForwardedInboxTailSequenceNumber = await WaitForInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, DefaultNamespace, expected: 1, timeout: TimeSpan.FromSeconds(30));
        }

        async Task AppendToSourceOutbox(EventSourceId eventSourceId, ReconnectForwardedEvent @event)
        {
            // Only the source outbox is forwarded to subscribers, and only public event types may be appended to it.
            var source = await ChronicleClient.GetEventStore(SourceEventStoreName);
            var result = await source.GetEventSequence(EventSequenceId.Outbox).Append(eventSourceId, @event);
            result.IsSuccess.ShouldBeTrue();
        }

        void Destroy()
        {
            _client?.EvictEventStores();
            _client?.Dispose();
        }

        sealed class ExplicitArtifacts() : DefaultClientArtifactsProvider(new NoAssemblies())
        {
            public override IEnumerable<Type> EventTypes => [typeof(ReconnectForwardedEvent)];
        }

        sealed class NoAssemblies : ICanProvideAssembliesForDiscovery
        {
            public IEnumerable<Assembly> Assemblies => [];
            public IEnumerable<Type> DefinedTypes => [];

            public void Initialize()
            {
            }
        }
    }

    [EventType]
    [Public]
    public record ReconnectForwardedEvent(string Value);

    [Fact] void should_forward_the_event_appended_after_reconnect_to_the_inbox() => Context.ForwardedInboxTailSequenceNumber.Value.ShouldEqual(1UL);
}
