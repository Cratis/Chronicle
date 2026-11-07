// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationIdentitySerializer;

public class when_deserializing_malformed_identities : Specification
{
    Exception[] _errors;

    void Because()
    {
        var eventLogKey = EventSequenceMutationIdentity.TryCreate("event-log").Identity!.Key.Snapshot();
        var otherKey = EventSequenceMutationIdentity.TryCreate("other").Identity!.Key.Snapshot();
        _errors =
        [
            Catch.Exception(() => Deserialize(new("key", new BsonBinaryData(eventLogKey)))),
            Catch.Exception(() => Deserialize(new BsonDocument { ["display"] = 42, ["key"] = new BsonBinaryData(eventLogKey) })),
            Catch.Exception(() => Deserialize(new("display", "event-log"))),
            Catch.Exception(() => Deserialize(new BsonDocument { ["display"] = "event-log", ["key"] = "event-log" })),
            Catch.Exception(() => Deserialize(new BsonDocument { ["display"] = "event-log", ["key"] = new BsonBinaryData(otherKey) })),
            Catch.Exception(() => Deserialize(new BsonDocument { ["display"] = "event-log", ["key"] = new BsonBinaryData([0xff]) }))
        ];
    }

    [Fact] void should_fail_every_malformed_shape_with_the_typed_identity_error() => _errors.All(_ => _ is InvalidEventSequenceIdentityKey).ShouldBeTrue();

    static EventSequenceMutationIdentity Deserialize(BsonDocument document)
    {
        using var reader = new BsonDocumentReader(document);
        return new EventSequenceMutationIdentitySerializer().Deserialize(BsonDeserializationContext.CreateRoot(reader), default);
    }
}
