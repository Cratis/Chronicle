// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_a_hosted_peer_is_admitted : given.a_pull_service
{
    FederatedEventBatch _named;
    FederatedEventBatch _unnamed;

    void Establish()
    {
        AdmitHostedPeer(allPublic: false, eventTypes: ["order-placed"]);
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        RegisterEventType("order-shipped", EventTypeVisibility.Public);
        RegisterEventType("order-internal", EventTypeVisibility.Private);
        Append("order-placed", 0);
        Append("order-shipped", 1);
        Append("order-internal", 2);
    }

    async Task Because()
    {
        _named = await Pull();
        _unnamed = await Pull(eventStore: string.Empty, @namespace: string.Empty);
    }

    [Fact] void should_be_ok() => _named.Status.ShouldEqual(FederatedEventBatchStatus.Ok);
    [Fact] void should_only_deliver_what_the_policy_allows() => _named.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(0UL);
    [Fact] void should_serve_the_target_of_the_policy_when_the_request_leaves_it_out() => _unnamed.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(0UL);
    [Fact] void should_not_ask_for_pinned_grants_to_cover_it() => _grants.ShouldBeEmpty();
}
