// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_authorizing_a_new_lifecycle;

public class and_legacy_authorization_loses_replica_set_topology : racing_encryption_key_collections
{
    void Establish()
    {
        _fence = new("identifier", 1u, [], false, Version: 1, ErasedLifecycle: 1);
        _topology.IsStandalone.Returns(false, true);
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_fence));
    }

    Task Because() => _storage.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier");

    [Fact] void should_read_linearly() => _erasures.Received(2).WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_never_downgrade_reads() => _erasures.DidNotReceive().WithReadConcern(ReadConcern.Default);
    [Fact] void should_write_with_majority_journal_and_timeout() => _erasures.Received(1).WithWriteConcern(Arg.Is<WriteConcern>(_ => _.W.Equals(WriteConcern.WMajority.W) && _.Journal == true && _.WTimeout == new EncryptionKeyErasureOptions().FenceTimeout));
    [Fact] void should_never_downgrade_writes() => _erasures.DidNotReceive().WithWriteConcern(WriteConcern.Acknowledged);
}
