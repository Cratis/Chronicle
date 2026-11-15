// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage;

public class when_the_server_is_standalone : racing_encryption_key_collections
{
    EncryptionKeyErasure? _fence;

    void Establish() => _topology.IsStandalone.Returns(true);

    async Task Because() => _fence = await _storage.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier");

    [Fact] void should_promise_no_fence_guarantee() => _storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
    [Fact] void should_not_claim_a_durable_external_fence() => _storage.IsDurable.ShouldBeFalse();
    [Fact] void should_still_read_the_fence() => _fence.ShouldNotBeNull();
    [Fact] void should_read_with_the_server_default_concern() => _erasures.Received().WithReadConcern(ReadConcern.Default);
    [Fact] void should_never_ask_for_a_linearizable_read() => _erasures.DidNotReceive().WithReadConcern(ReadConcern.Linearizable);
}
