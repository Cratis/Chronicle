// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

public class and_the_listing_contains_the_system_store : given.a_pii_manager
{
    void Establish() => _storage.GetEventStores().Returns([EventStore, EventStoreName.System, (EventStoreName)"system"]);

    Task Because() => _manager.DeleteEncryptionKeyFor(Identifier);

    [Fact] void should_erase_the_requested_store() => _keyStore.Received(1).DeleteThroughFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<string>());
    [Fact] void should_never_fence_the_system_store() => _keyStore.DidNotReceive().RecordErasureFor(Arg.Is<EventStoreName>(store => store.Value.Equals("system", StringComparison.OrdinalIgnoreCase)), EventStoreNamespace, Identifier, Arg.Any<Storage.Compliance.EncryptionKeyErasureRequest>());
    [Fact] void should_never_erase_the_system_store() => _keyStore.DidNotReceive().DeleteThroughFor(Arg.Is<EventStoreName>(store => store.Value.Equals("system", StringComparison.OrdinalIgnoreCase)), EventStoreNamespace, Identifier, Arg.Any<string>());
}
