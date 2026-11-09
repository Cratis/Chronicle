// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_authorizing_a_new_lifecycle;

public class and_legacy_authorization_uses_a_standalone : racing_encryption_key_collections
{
    Exception _result;

    void Establish()
    {
        _topology.IsStandalone.Returns(true);
        _fence = new("identifier", 1u, [], false, Version: 1, ErasedLifecycle: 1);
        _erasures.WithReadConcern(ReadConcern.Linearizable).Throws(new InvalidOperationException("Standalone servers cannot perform linearizable reads."));
        _erasures.WithWriteConcern(Arg.Is<WriteConcern>(_ => !_.Equals(WriteConcern.Acknowledged))).Throws(new InvalidOperationException("Standalone authorization must use acknowledged writes."));
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_fence));
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier"));

    [Fact] void should_authorize_without_failing() => _result.ShouldBeNull();
    [Fact] void should_read_with_default_concerns() => _erasures.Received(2).WithReadConcern(ReadConcern.Default);
    [Fact] void should_never_read_linearly() => _erasures.DidNotReceive().WithReadConcern(ReadConcern.Linearizable);
    [Fact] void should_write_with_acknowledged_concerns() => _erasures.Received(1).WithWriteConcern(WriteConcern.Acknowledged);
}
