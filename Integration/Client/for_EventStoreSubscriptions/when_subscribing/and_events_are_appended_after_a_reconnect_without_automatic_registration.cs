// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
            await AppendEvent(SourceEventStoreName, "first", new TestEvent("before"));
            var firstTail = await WaitForInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, DefaultNamespace, expected: 0, timeout: TimeSpan.FromSeconds(30));
            firstTail.Value.ShouldEqual(0UL);

            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();

            await AppendEvent(SourceEventStoreName, "second", new TestEvent("after"));
            ForwardedInboxTailSequenceNumber = await WaitForInboxTailSequenceNumber(SourceEventStoreName, TargetEventStoreName, DefaultNamespace, expected: 1, timeout: TimeSpan.FromSeconds(30));
        }

        void Destroy()
        {
            _client?.EvictEventStores();
            _client?.Dispose();
        }

        sealed class ExplicitArtifacts() : DefaultClientArtifactsProvider(new NoAssemblies())
        {
            public override IEnumerable<Type> EventTypes => [typeof(TestEvent)];
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

    [Fact] void should_forward_the_event_appended_after_reconnect_to_the_inbox() => Context.ForwardedInboxTailSequenceNumber.Value.ShouldEqual(1UL);
}
