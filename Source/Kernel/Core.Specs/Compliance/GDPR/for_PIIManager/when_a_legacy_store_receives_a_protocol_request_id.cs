// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager;

public class when_a_legacy_store_receives_a_protocol_request_id : given.a_pii_manager
{
    Exception? _error;

    void Establish() => _keyStore.FenceGuarantees.Returns(EncryptionKeyFenceGuarantees.None);

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyInWithRequest(Identifier, [EventStore], "retry"));

    [Fact] void should_refuse_a_confirmation_guarantee_it_cannot_provide() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_not_replay_an_unbounded_delete() => _keyStore.DidNotReceive().DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_record_an_unbounded_fence() => _keyStore.DidNotReceive().RecordErasureFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_claim_confirmation() => _keyStore.DidNotReceive().ConfirmErasureFor(EventStore, EventStoreNamespace, Identifier, "retry");
}
