// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager;

public class when_erasing_with_a_legacy_store : given.a_pii_manager
{
    bool _erased;

    void Establish()
    {
        // HELD Vault and Azure without a durable authority implement only the release-N erasure contract.
        _keyStore.FenceGuarantees.Returns(EncryptionKeyFenceGuarantees.None);
        _keyStore.DeleteThroughFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<string>())
            .Returns(Task.FromException(new EncryptionKeyFenceUnavailable(Identifier)));
        _keyStore.When(_ => _.DeleteFor(EventStore, EventStoreNamespace, Identifier)).Do(_ => _erased = true);
    }

    async Task Because()
    {
        await _manager.DeleteEncryptionKeyIn(Identifier, [EventStore]);
        await _manager.AllowNewEncryptionKeyIn(Identifier, [EventStore]);
    }

    [Fact] void should_sweep_the_legacy_key() => _erased.ShouldBeTrue();
    [Fact] void should_record_the_legacy_fence() => _keyStore.Received(1).RecordErasureFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_call_the_unsupported_bounded_sweep() => _keyStore.DidNotReceive().DeleteThroughFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<string>());
    [Fact] void should_not_claim_protocol_confirmation() => _keyStore.DidNotReceive().ConfirmErasureFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<string>());
    [Fact] void should_not_wait_for_protocol_grace() => _delays.ShouldBeEmpty();
    [Fact] void should_preserve_legacy_successor_authorization() => _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_evict_the_cached_key() => _cacheClient.Received(2).Evict(EventStore, EventStoreNamespace, Identifier);
}
