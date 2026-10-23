// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_recording_an_erasure;

public class and_a_racing_erasure_was_stored_first : racing_encryption_key_collections
{
    Exception _result;

    void Establish() =>
        _erasures.ReplaceOneAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<EncryptionKeyErasureForIdentifier>(), Arg.Any<ReplaceOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReplaceOneResult>(duplicate_keys.Duplicate()));

    async Task Because() => _result = await Catch.Exception(() => _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));

    [Fact] void should_surface_the_duplicate_key_error() => _result.ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_not_overwrite_the_stored_erasure() => _erasures.ReceivedWithAnyArgs(1).ReplaceOneAsync(default(FilterDefinition<EncryptionKeyErasureForIdentifier>)!, default!, default(ReplaceOptions)!, default);
}
