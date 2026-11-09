// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_confirming_an_erasure;

public class and_topology_is_discovered_during_sweep_verification : given.racing_encryption_key_collections
{
    readonly TaskCompletionSource<IAsyncCursor<BsonDocument>> _probe = new();
    Exception _exception;
    bool _sweepStarted;

    void Establish()
    {
        _fence = new("identifier", 7u, [], false, 1, Requests: [new("request", 7u, 1)]);
        _keys.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyForIdentifier, BsonDocument>>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _sweepStarted = true;
            return _probe.Task;
        });
    }

    async Task Because()
    {
        var confirmation = Catch.Exception(() => _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", "request"));
        _sweepStarted.ShouldBeTrue();
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
