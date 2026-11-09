// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_choosing_concerns;

public class on_a_replica_set : racing_encryption_key_collections
{
    void Establish() => _topology.IsStandalone.Returns(false);

    async Task Because() => await _storage.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier");

    [Fact] void should_write_with_majority() => _storage.Durability.W.ShouldEqual(WriteConcern.WMajority.W);
    [Fact] void should_write_with_journal() => _storage.Durability.Journal.ShouldEqual(true);
    [Fact] void should_bound_the_write_wait() => _storage.Durability.WTimeout.ShouldNotBeNull();
    [Fact] void should_read_linearizable() => _storage.Linearizability.ShouldEqual(ReadConcern.Linearizable);
    [Fact] void should_delete_keys_with_the_durable_concern() => _keys.Received().WithWriteConcern(_storage.Durability);
}
