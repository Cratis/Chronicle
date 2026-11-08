// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage;

public class when_the_server_is_a_replica_set : racing_encryption_key_collections
{
    async Task Because() => await _storage.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier");

    [Fact] void should_promise_the_fence_guarantees() => _storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);
    [Fact] void should_claim_a_durable_external_fence() => _storage.IsDurable.ShouldBeTrue();
    [Fact] void should_read_linearizably() => _erasures.Received().WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_bound_majority_writes_by_the_fence_timeout() => _storage.Durability.WTimeout.ShouldEqual(new EncryptionKeyErasureOptions().FenceTimeout);
}
