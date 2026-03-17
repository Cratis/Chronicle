// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_not_already_subscribed.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_not_already_subscribed(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture)
        : given.subscription_states_after_subscribing(chronicleInProcessFixture)
    {
        public readonly EventStoreSubscriptionId SubscriptionId = "test-subscription";
        public readonly string SourceEventStore = "source-event-store";

        public Task Because() =>
            Subscribe((SubscriptionId, SourceEventStore, default));
    }

    [Fact]
    void should_have_stored_one_subscription() =>
        Context.StoredSubscriptions.Count().ShouldEqual(1);

    [Fact]
    void should_have_correct_subscription_id() =>
        Context.StoredSubscriptions.Single().Identifier.Value.ShouldEqual(Context.SubscriptionId.Value);

    [Fact]
    void should_have_correct_source_event_store() =>
        Context.StoredSubscriptions.Single().SourceEventStore.Value.ShouldEqual(Context.SourceEventStore);
}
