// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_confirming_an_erasure;

public class and_topology_can_change_while_selecting_concerns : given.racing_encryption_key_collections
{
    void Establish()
    {
        _fence = new("identifier", 7u, [], false, 1, Requests: [new("request", 7u, 1)]);
        _topology.IsStandalone.Returns(false, false, false, false, true);
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(_fence));
    }

    Task Because() => _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", "request");

    [Fact] void should_read_the_fence_linearly() => _erasures.Received().WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_never_downgrade_fence_reads() => _erasures.DidNotReceive().WithReadConcern(Arg.Is<ReadConcern>(_ => _ != ReadConcern.Linearizable));
    [Fact] void should_read_from_the_primary() => _erasures.Received().WithReadPreference(ReadPreference.Primary);
    [Fact] void should_verify_the_sweep_linearly() => _keys.Received().WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_verify_the_sweep_on_the_primary() => _keys.Received().WithReadPreference(ReadPreference.Primary);
    [Fact] void should_write_with_majority_journal_and_timeout() => _erasures.Received().WithWriteConcern(Arg.Is<WriteConcern>(_ => _.W.Equals(WriteConcern.WMajority.W) && _.Journal == true && _.WTimeout != null));
    [Fact] void should_never_downgrade_writes() => _erasures.DidNotReceive().WithWriteConcern(WriteConcern.Acknowledged);
}
