// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_subscribing.and_subscription_is_persisted.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_subscription_is_persisted(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.multi_event_store_subscription_setup(fixture)
    {
        public string SubscriptionId { get; } = "persisted-subscription";
        public IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> PersistedSubscriptions { get; private set; } = [];

        async Task Because()
        {
            // Create subscription using simpler pattern without reactor wait
            var targetEventStore = await ChronicleClient.GetEventStore(TargetEventStoreName);
            await targetEventStore.Subscriptions.Subscribe(
                new EventStoreSubscriptionId(SubscriptionId),
                SourceEventStoreName);

            // Retrieve stored subscriptions from target event store
            PersistedSubscriptions = await GetStoredSubscriptions(TargetEventStoreName);
        }
    }

    [Fact]
    void should_have_stored_one_subscription() =>
        Context.PersistedSubscriptions.Count().ShouldEqual(1);

    [Fact]
    void should_have_correct_subscription_id() =>
        Context.PersistedSubscriptions.Single().Identifier.Value.ShouldEqual(Context.SubscriptionId);

    [Fact]
    void should_have_correct_source_event_store() =>
        Context.PersistedSubscriptions.Single().SourceEventStore.Value.ShouldEqual(Context.SourceEventStoreName);
}
