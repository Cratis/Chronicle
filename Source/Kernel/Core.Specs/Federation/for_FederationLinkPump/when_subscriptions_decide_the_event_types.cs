// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_subscriptions_decide_the_event_types : given.a_link_pump
{
    void Establish()
    {
        _subscriptions.Subscriptions.Clear();
        _subscriptions.Subscriptions.Add(new(["type-b", "type-a"], []));
        _subscriptions.Subscriptions.Add(new(["type-c", "type-a"], []));

        // The producer answers with an unsubscribed type too; the consumer still decides.
        _pull.Enqueue(given.FakePull.Batch("remote", 3, 2, given.FakePull.Event(1, "type-c"), given.FakePull.Event(2, "type-z")));
    }

    async Task Because() => await _pump.Step(default);

    [Fact] void should_request_the_union_of_the_subscribed_event_types() => _pull.Requests[0].EventTypes.ShouldEqual(["type-a", "type-b", "type-c"]);
    [Fact] void should_append_only_subscribed_types() => _inbox.Appended.Select(_ => _.SequenceNumber).ShouldEqual([1UL]);
    [Fact] void should_skip_the_unsubscribed_type() => _metrics.Received(1).EventSkipped(BindingId, FederationSkipReasons.Filtered);
}
