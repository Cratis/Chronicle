// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_getting_the_fence;

public class and_the_primary_cannot_serve_a_strong_read : racing_encryption_key_collections
{
    Exception _result;

    void Establish() =>
        _erasures.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyErasureForIdentifier, EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IAsyncCursor<EncryptionKeyErasureForIdentifier>>(new TimeoutException("primary unavailable")));

    async Task Because() => _result = await Catch.Exception(() => _storage.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));

    [Fact] void should_report_unavailable_instead_of_an_empty_fence() => _result.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_never_fall_back_to_majority_reads() => _erasures.DidNotReceive().WithReadConcern(ReadConcern.Majority);
}
