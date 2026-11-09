// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_authorizing_a_new_lifecycle;

public class and_an_identical_authorization_wins_the_compare_and_swap : racing_encryption_key_collections
{
    Exception _result;
    int _writes;

    void Establish()
    {
        _fence = new("identifier", 1u, [], false, Version: 1, ErasedLifecycle: 1);
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _writes++;
                _fence = _fence with { Version = 2, Lifecycle = 2, NewKeyAllowed = true };
                return Task.FromResult<EncryptionKeyErasureForIdentifier>(null!);
            });
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", 1, 1));

    [Fact] void should_accept_the_identical_authorization() => _result.ShouldBeNull();
    [Fact] void should_not_write_again() => _writes.ShouldEqual(1);
    [Fact] void should_keep_the_authorized_lifecycle() => _fence.Lifecycle.ShouldEqual(2u);
}
