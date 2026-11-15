// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_recording_an_erasure;

public class and_the_write_response_is_lost : racing_encryption_key_collections
{
    Exception _result;

    void Establish() =>
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EncryptionKeyErasureForIdentifier>(new TimeoutException("lost response")));

    async Task Because() => _result = await Catch.Exception(() => _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new("request")));

    [Fact] void should_surface_the_unknown_outcome() => _result.ShouldBeOfExactType<EncryptionKeyFenceWriteOutcomeUnknown>();
    [Fact] void should_not_retry_an_ambiguous_write() => _erasures.Received(1).FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>());
}
