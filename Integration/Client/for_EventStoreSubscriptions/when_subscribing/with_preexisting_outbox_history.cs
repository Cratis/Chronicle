// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_subscribing.with_preexisting_outbox_history.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_subscribing;

[Collection(ChronicleCollection.Name)]
public class with_preexisting_outbox_history(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_subscription_with_retained_outbox_history(fixture)
    {
        async Task Because()
        {
            await SubscribeAndDeliverHistory();
            await ReadInbox();
        }
    }

    [Fact] void should_have_retained_history_starting_at_zero() => Context.FirstSeedNumber.Value.ShouldEqual(0UL);
    [Fact] void should_store_both_historical_events_once() => Context.StoredEvents.Count.ShouldEqual(2);
    [Fact] void should_forward_the_actual_historical_values() => Context.StoredValues().ShouldContainOnly(["first", "second"]);
    [Fact] void should_preserve_both_event_sources() => Context.StoredEvents.Select(_ => _.Context.EventSourceId.Value).ShouldContainOnly(["first-source", "second-source"]);
}
