// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_a_hosted_peer_addresses_another_target : given.a_pull_service
{
    FederatedEventBatch _otherStore;
    FederatedEventBatch _otherNamespace;

    void Establish()
    {
        AdmitHostedPeer(allPublic: true);
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        Append("order-placed", 0);
    }

    async Task Because()
    {
        _otherStore = await Pull(eventStore: "another-customers-store");
        _otherNamespace = await Pull(@namespace: "another-customers-namespace");
    }

    [Fact] void should_deny_another_event_store() => _otherStore.Status.ShouldEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_deliver_nothing_from_another_event_store() => _otherStore.Events.ShouldBeEmpty();
    [Fact] void should_deny_another_namespace() => _otherNamespace.Status.ShouldEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_deliver_nothing_from_another_namespace() => _otherNamespace.Events.ShouldBeEmpty();
}
