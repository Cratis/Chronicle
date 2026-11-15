// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_recording_an_erasure;

public class and_the_write_cannot_select_a_server : racing_encryption_key_collections
{
    Exception _result;

    void Establish() =>
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EncryptionKeyErasureForIdentifier>(new TimeoutException("A timeout occurred after 30000ms selecting a server using CompositeServerSelector.")));

    async Task Because() => _result = await Catch.Exception(() => _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new("request")));

    [Fact] void should_report_the_fence_unavailable_as_nothing_was_sent() => _result.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
}
