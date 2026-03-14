// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_subscription_persists_after_client_reconnects.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_subscription_persists_after_client_reconnects(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture)
        : given.subscription_states_after_subscribing(chronicleInProcessFixture)
    {
        public readonly EventStoreSubscriptionId SubscriptionId = "persistent-subscription";
        public readonly string SourceEventStore = "another-event-store";
        public IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> StoredSubscriptionsAfterReconnect { get; private set; } = [];

        public async Task Because()
        {
            await Subscribe((SubscriptionId, SourceEventStore, default));
            StoredSubscriptionsAfterReconnect = await EventStoreStorage.EventStoreSubscriptions.GetAll();
        }
    }

    [Fact]
    void should_have_the_subscription_stored() =>
        Context.StoredSubscriptionsAfterReconnect.Count().ShouldEqual(1);

    [Fact]
    void should_have_the_correct_subscription_id() =>
        Context.StoredSubscriptionsAfterReconnect.Single().Identifier.Value.ShouldEqual(Context.SubscriptionId.Value);

    [Fact]
    void should_have_the_correct_source_event_store() =>
        Context.StoredSubscriptionsAfterReconnect.Single().SourceEventStore.Value.ShouldEqual(Context.SourceEventStore);
}
