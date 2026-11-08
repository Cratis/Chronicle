// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_getting_or_adding_with_revision;

public class and_a_new_key_is_published : racing_encryption_key_collections
{
    readonly List<string> _operations = [];
    EncryptionKeyForIdentifier _stage;
    EncryptionKeyAtRevision _result;

    void Establish()
    {
        _erasures.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyErasureForIdentifier, EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _operations.Add("read-fence");
                return Task.FromResult(duplicate_keys.CursorOf(_fence));
            });
        _keys.InsertOneAsync(Arg.Any<EncryptionKeyForIdentifier>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _stage = call.Arg<EncryptionKeyForIdentifier>();
                _operations.Add("stage");
                return Task.CompletedTask;
            });
        _keys.UpdateOneAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _operations.Add("publish");
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
            });
    }

    async Task Because() => _result = await _storage.GetOrAddWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new EncryptionKey([1], [2]));

    [Fact] void should_validate_between_staging_and_publication() => _operations.ShouldEqual(["read-fence", "stage", "read-fence", "publish"]);
    [Fact] void should_stage_an_unpublished_row() => _stage.Published.ShouldBeFalse();
    [Fact] void should_attach_the_observed_lifecycle() => _stage.StagedLifecycle.ShouldEqual(1u);
    [Fact] void should_return_the_persisted_lifecycle() => _result.StagedLifecycle.ShouldEqual(_stage.StagedLifecycle);
    [Fact] void should_assign_a_writer_nonce() => string.IsNullOrEmpty(_stage.WriterNonce).ShouldBeFalse();
    [Fact] void should_use_majority_write_concern() => _keys.Received().WithWriteConcern(WriteConcern.WMajority);
}
