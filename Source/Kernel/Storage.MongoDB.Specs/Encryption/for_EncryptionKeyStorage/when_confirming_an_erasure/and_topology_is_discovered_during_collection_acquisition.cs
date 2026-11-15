// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_confirming_an_erasure;

public class and_topology_is_discovered_during_collection_acquisition : given.racing_encryption_key_collections
{
    readonly TaskCompletionSource<IAsyncCursor<BsonDocument>> _probe = new();
    Exception _exception;

    void Establish() => _keys.Indexes.ListAsync(Arg.Any<CancellationToken>()).Returns(_probe.Task);

    async Task Because()
    {
        var confirmation = Catch.Exception(() => _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", "erase"));
        confirmation.IsCompleted.ShouldBeFalse();
        _topology.IsStandalone.Returns(true);
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.Current.Returns([]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
        _probe.SetResult(cursor);
        _exception = await confirmation;
    }

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_persist_confirmation() => _erasures.DidNotReceive().FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>());
}
