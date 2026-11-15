// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_saving_a_key;

public class and_an_implicit_revision_loses_an_insert_race : racing_encryption_key_collections
{
    readonly List<EncryptionKeyForIdentifier> _attempts = [];
    Exception _result;

    void Establish() =>
        _keys.InsertOneAsync(Arg.Any<EncryptionKeyForIdentifier>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _attempts.Add(call.Arg<EncryptionKeyForIdentifier>());
                if (_attempts.Count == 1)
                {
                    _keys.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyForIdentifier, EncryptionKeyForIdentifier>>(), Arg.Any<CancellationToken>())
                        .Returns(_ => Task.FromResult(duplicate_keys.CursorOf(new EncryptionKeyForIdentifier(new("identifier", 1u), [3], [4]))));
                    return Task.FromException(duplicate_keys.Duplicate());
                }

                return Task.CompletedTask;
            });

    async Task Because() => _result = await Catch.Exception(() => _storage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new EncryptionKey([1], [2])));

    [Fact] void should_retry_a_definite_insert_conflict() => _attempts.Count.ShouldEqual(2);
    [Fact] void should_choose_a_new_revision() => _attempts[1].Id.Revision.Value.ShouldEqual(2u);
    [Fact] void should_keep_both_attempts_unpublished_until_validation() => _attempts.TrueForAll(_ => !_.Published).ShouldBeTrue();
    [Fact] void should_complete_publication() => _result.ShouldBeNull();
}
