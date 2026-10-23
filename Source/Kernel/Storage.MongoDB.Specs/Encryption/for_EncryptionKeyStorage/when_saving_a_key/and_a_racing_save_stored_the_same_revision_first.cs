// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_saving_a_key;

public class and_a_racing_save_stored_the_same_revision_first : racing_encryption_key_collections
{
    Exception _result;

    void Establish() =>
        _keys.ReplaceOneAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<EncryptionKeyForIdentifier>(), Arg.Any<ReplaceOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReplaceOneResult>(duplicate_keys.Duplicate()));

    async Task Because() => _result = await Catch.Exception(() => _storage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new EncryptionKey([1], [2])));

    [Fact] void should_surface_the_duplicate_key_error() => _result.ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_not_overwrite_the_stored_key() => _keys.ReceivedWithAnyArgs(1).ReplaceOneAsync(default!, default!, default(ReplaceOptions)!, default);
}
