// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_authorizing_a_new_lifecycle;

public class and_protocol_authorization_uses_a_standalone : racing_encryption_key_collections
{
    Exception _result;

    void Establish()
    {
        _topology.IsStandalone.Returns(true);
        _fence = new("identifier", 1u, [], false, Version: 1, ErasedLifecycle: 1);
        _erasures.WithReadConcern(ReadConcern.Linearizable).Throws(new MongoException("Standalone servers cannot perform linearizable reads."));
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", 1, 1));

    [Fact] void should_fail_closed() => _result.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_require_a_linearizable_read() => _erasures.Received(1).WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_never_downgrade_reads() => _erasures.DidNotReceive().WithReadConcern(ReadConcern.Default);
    [Fact] void should_not_authorize_a_new_key() => _erasures.DidNotReceive().WithWriteConcern(Arg.Any<WriteConcern>());
}
