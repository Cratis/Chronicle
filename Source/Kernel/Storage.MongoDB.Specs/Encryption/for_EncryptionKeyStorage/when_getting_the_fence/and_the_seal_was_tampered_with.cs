// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_getting_the_fence;

public class and_the_seal_was_tampered_with : racing_encryption_key_collections
{
    Exception _result;

    void Establish()
    {
        _sealFence = false;
        _fence = new("identifier", 7u, [], false, 1, Seal: "invalid-seal");
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));

    [Fact] void should_refuse_to_attest_the_fence() => _result.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_read_linearly() => _erasures.Received(1).WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_read_from_the_primary() => _erasures.Received(1).WithReadPreference(ReadPreference.Primary);
    [Fact] void should_bound_the_read_time() => _erasures.Received(1).FindAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Is<FindOptions<EncryptionKeyErasureForIdentifier, EncryptionKeyErasureForIdentifier>>(_ => _.MaxTime == new EncryptionKeyErasureOptions().FenceTimeout), Arg.Any<CancellationToken>());
}
