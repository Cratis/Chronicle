// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_recording_an_erasure;

public class and_authorization_wins_a_compare_and_swap_race : racing_encryption_key_collections
{
    Exception _result;
    int _writes;

    void Establish() =>
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _writes++;

                // Another erasure and its authorization commit after this request observed lifecycle one.
                _fence = new("identifier", 1u, [], true, Version: 2, Lifecycle: 2, ErasedLifecycle: 1, Requests: [new("winner", 1u, 1)]);
                return Task.FromResult<EncryptionKeyErasureForIdentifier>(null!);
            });

    async Task Because() => _result = await Catch.Exception(() => _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));

    [Fact] void should_reject_the_stale_erasure() => _result.ShouldBeOfExactType<EncryptionKeyFenceChanged>();
    [Fact] void should_not_retry_against_the_authorized_successor() => _writes.ShouldEqual(1);
    [Fact] void should_keep_the_successor_authorized() => _fence.NewKeyAllowed.ShouldBeTrue();
    [Fact] void should_not_record_the_losing_request() => _fence.Requests.Select(_ => _.RequestId).ShouldEqual(["winner"]);
}
