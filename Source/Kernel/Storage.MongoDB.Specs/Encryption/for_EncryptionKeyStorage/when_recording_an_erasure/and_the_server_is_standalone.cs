// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_recording_an_erasure;

public class and_the_server_is_standalone : racing_encryption_key_collections
{
    Exception _recording;
    Exception _confirmation;
    EncryptionKeyErasureRequest _pending;

    void Establish()
    {
        _topology.IsStandalone.Returns(true);
        _records.WithReadConcern(ReadConcern.Linearizable).Throws(new InvalidOperationException("Standalone servers cannot perform linearizable reads."));
        _erasures.FindOneAndUpdateAsync(Arg.Any<FilterDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<UpdateDefinition<EncryptionKeyErasureForIdentifier>>(), Arg.Any<FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var registry = BsonSerializer.SerializerRegistry;
                var update = call.Arg<UpdateDefinition<EncryptionKeyErasureForIdentifier>>().Render(new(registry.GetSerializer<EncryptionKeyErasureForIdentifier>(), registry)).AsBsonDocument;
                _pending = BsonSerializer.Deserialize<EncryptionKeyErasureRequest>(update["$push"].AsBsonDocument.GetElement(0).Value.AsBsonDocument);
                return Task.FromResult(_fence);
            });
    }

    async Task Because()
    {
        _recording = await Catch.Exception(() => _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", new("request")));
        _confirmation = await Catch.Exception(() => _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", "request"));
    }

    [Fact] void should_record_without_failing() => _recording.ShouldBeNull();
    [Fact] void should_record_the_request_as_pending() => _pending.Confirmed.ShouldBeFalse();
    [Fact] void should_preserve_the_retry_identity() => _pending.RequestId.ShouldEqual("request");
    [Fact] void should_use_default_concerns_for_the_archive_lookup() => _records.Received().WithReadConcern(ReadConcern.Default);
    [Fact] void should_advertise_no_fence_guarantees() => _storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
    [Fact] void should_fail_closed_when_confirming() => _confirmation.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
}
