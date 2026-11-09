// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage;

public class when_a_degraded_read_finds_a_confirmation : given.racing_encryption_key_collections
{
    Exception _exception;

    void Establish()
    {
        _topology.IsStandalone.Returns(true);
        _fence = new("identifier", 7u, [], false, 1, Requests: [new("request", 7u, 1, Confirmed: true)]);
        _erasures.WithReadConcern(ReadConcern.Linearizable).Returns(_ => throw new MongoException("linearizable reads are unavailable"));
    }

    async Task Because() => _exception = await Catch.Exception(() => _storage.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));

    [Fact] void should_not_expose_the_confirmation() => _exception.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_require_a_strong_read() => _erasures.Received().WithReadConcern(ReadConcern.Linearizable);
}
