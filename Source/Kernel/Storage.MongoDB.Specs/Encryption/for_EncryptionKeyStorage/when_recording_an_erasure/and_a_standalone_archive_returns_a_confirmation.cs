// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_recording_an_erasure;

public class and_a_standalone_archive_returns_a_confirmation : racing_encryption_key_collections
{
    Exception _exception;

    void Establish()
    {
        _topology.IsStandalone.Returns(true);
        _records.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf(new BsonDocument("_id", "archived-request"))));
        _records.WithReadConcern(ReadConcern.Linearizable).Throws(new InvalidOperationException("Standalone servers cannot perform linearizable reads."));
    }

    async Task Because() => _exception = await Catch.Exception(() => _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new("request")));

    [Fact] void should_not_trust_the_degraded_confirmation() => _exception.ShouldNotBeNull();
    [Fact] void should_require_an_authoritative_read() => _records.Received().WithReadConcern(ReadConcern.Linearizable);
    [Fact] Task should_not_write_a_confirmation() => _erasures.DidNotReceive().FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>());
}
