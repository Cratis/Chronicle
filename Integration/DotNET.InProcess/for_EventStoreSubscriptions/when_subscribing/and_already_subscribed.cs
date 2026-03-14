// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using context = Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing.and_already_subscribed.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class and_already_subscribed(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture)
        : given.subscription_states_after_subscribing(chronicleInProcessFixture)
    {
        public readonly EventStoreSubscriptionId SubscriptionId = "test-subscription";
        public readonly string SourceEventStore = "source-event-store";

        public async Task Because()
        {
            await Subscribe((SubscriptionId, SourceEventStore, default));
            await Subscribe((SubscriptionId, SourceEventStore, default));
        }
    }

    [Fact]
    void should_still_have_only_one_stored_subscription() =>
        Context.StoredSubscriptions.Count().ShouldEqual(1);

    [Fact]
    void should_have_correct_subscription_id() =>
        Context.StoredSubscriptions.Single().Identifier.Value.ShouldEqual(Context.SubscriptionId.Value);
}
