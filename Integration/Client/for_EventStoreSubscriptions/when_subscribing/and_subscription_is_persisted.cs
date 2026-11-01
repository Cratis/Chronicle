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
        public IEnumerable<EventStoreSubscriptionDefinition> PersistedSubscriptions { get; private set; } = [];

        async Task Because()
        {
            // The setup's Subscribe waits until the target's subscriptions reactor has processed the subscription.
            await Subscribe(SubscriptionId, SourceEventStoreName, TargetEventStoreName);

            // The target store is shared with other classes in the collection, and the kernel reset between them can
            // leave their subscriptions behind (Cratis/Chronicle#5402). Only this class's own id is its subject;
            // a duplicate of it still shows.
            var all = await GetStoredSubscriptions(TargetEventStoreName);
            PersistedSubscriptions = all.Where(_ => _.Id.Value == SubscriptionId).ToArray();
        }
    }

    [Fact]
    void should_have_stored_one_subscription() =>
        Context.PersistedSubscriptions.Count().ShouldEqual(1);

    [Fact]
    void should_have_correct_subscription_id() =>
        Context.PersistedSubscriptions.Single().Id.Value.ShouldEqual(Context.SubscriptionId);

    [Fact]
    void should_have_correct_source_event_store() =>
        Context.PersistedSubscriptions.Single().SourceEventStore.ShouldEqual(Context.SourceEventStoreName);
}
