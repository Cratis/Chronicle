// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_confirming_an_erasure;

public class and_the_server_is_standalone : racing_encryption_key_collections
{
    Exception _result;

    void Establish()
    {
        _fence = new("identifier", 7u, [], false, 1, Requests: [new("request", 7u, 1)]);
        _topology.IsStandalone.Returns(true);
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", "request"));

    [Fact] void should_leave_the_erasure_pending() => _result.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_write_a_confirmation() => _erasures.DidNotReceive().FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>());
}
