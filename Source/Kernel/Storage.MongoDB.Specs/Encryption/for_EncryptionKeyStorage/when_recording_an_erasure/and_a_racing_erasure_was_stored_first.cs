// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_recording_an_erasure;

public class and_a_racing_erasure_was_stored_first : racing_encryption_key_collections
{
    Exception _result;
    int _writes;

    void Establish() =>
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _writes++;
                _fence = new("identifier", 7u, ["other-fingerprint"], false, 1, Requests: [new("other", 7u, 1)]);
                return Task.FromResult(_writes == 1 ? null! : _fence);
            });

    async Task Because() => _result = await Catch.Exception(() => _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new("request")));

    [Fact] void should_retry_the_definite_compare_and_swap_conflict() => _writes.ShouldEqual(2);
    [Fact] void should_complete_the_merge() => _result.ShouldBeNull();
    [Fact] void should_never_replace_the_stored_erasure() => _erasures.DidNotReceiveWithAnyArgs().ReplaceOneAsync(default!, default!, default(ReplaceOptions)!, default);
}
