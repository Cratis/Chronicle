// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_target_namespace_uses_different_casing : given.a_subscriber_with_events
{
    protected override EventStoreNamespaceName SourceNamespace => "cratis";

    void Establish() => _namespaceCasing.Canonicalize("Lobby", "cratis").Returns((EventStoreNamespaceName)"Cratis");

    async Task Because() => await Forward();

    [Fact] void should_use_the_target_stores_casing_for_the_inbox() => _inboxKey.ShouldEqual(new EventSequenceKey("inbox-Admin", "Lobby", "Cratis").ToString());
    [Fact] void should_forward_each_event_once() => AppendCalls().ShouldEqual(_events.Length);
}
