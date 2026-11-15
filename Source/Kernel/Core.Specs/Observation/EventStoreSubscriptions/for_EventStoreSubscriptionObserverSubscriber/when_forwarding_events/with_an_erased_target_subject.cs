// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_an_erased_target_subject : given.a_pinned_subscriber
{
    void Establish()
    {
        _encryptionKeyStorage.HasFor("Lobby", EventStoreNamespaceName.Default, Arg.Any<EncryptionKeyIdentifier>()).Returns(false);
        _encryptionKeyStorage.GetErasureForRelease("Lobby", EventStoreNamespaceName.Default, Arg.Any<EncryptionKeyIdentifier>()).Returns(new EncryptionKeyErasure(EncryptionKeyRevision.Initial, ["fingerprint"], NewKeyAllowed: false));
    }
    async Task Because() => _result = await Forward();
    [Fact] Task should_not_reinstate_the_erased_key() => _encryptionKeyStorage.DidNotReceive().GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>());
    [Fact] void should_still_use_the_requested_generation() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
}
