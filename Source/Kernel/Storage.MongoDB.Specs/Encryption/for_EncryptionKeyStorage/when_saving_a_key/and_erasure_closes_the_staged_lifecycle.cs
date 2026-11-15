// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_saving_a_key;

public class and_erasure_closes_the_staged_lifecycle : racing_encryption_key_collections
{
    EncryptionKeyForIdentifier _staged;
    BsonDocument _undo;
    Exception _result;

    void Establish()
    {
        _keys.InsertOneAsync(Arg.Any<EncryptionKeyForIdentifier>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _staged = call.Arg<EncryptionKeyForIdentifier>();
                _fence = new("identifier", 1u, [], false, 1, 1, 1, [new("erase", 1u, 1)]);
                return Task.CompletedTask;
            });
        _keys.DeleteOneAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var registry = BsonSerializer.SerializerRegistry;
                _undo = call.Arg<FilterDefinition<EncryptionKeyForIdentifier>>().Render(new(registry.GetSerializer<EncryptionKeyForIdentifier>(), registry));
                return Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1));
            });
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new EncryptionKey([1], [2]), 99u));

    [Fact] void should_stage_above_the_revision_floor() => _staged.Id.Revision.Value.ShouldEqual(99u);
    [Fact] void should_not_publish_the_stage_before_validation() => _staged.Published.ShouldBeFalse();
    [Fact] void should_reject_the_erased_lifecycle() => _result.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_undo_only_its_own_nonce() => _undo["writerNonce"].AsString.ShouldEqual(_staged.WriterNonce);
    [Fact] void should_undo_only_its_own_lifecycle() => _undo["stagedLifecycle"].ToInt32().ShouldEqual(1);
    [Fact] void should_never_publish_the_rejected_stage() => _keys.DidNotReceive().UpdateOneAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
}
