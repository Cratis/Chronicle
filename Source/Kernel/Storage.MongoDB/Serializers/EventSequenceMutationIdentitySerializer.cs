// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers;

/// <summary>
/// Represents a BSON serializer for <see cref="EventSequenceMutationIdentity"/>.
/// </summary>
public class EventSequenceMutationIdentitySerializer : SerializerBase<EventSequenceMutationIdentity>
{
    /// <inheritdoc/>
    public override EventSequenceMutationIdentity Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonDocumentSerializer.Instance.Deserialize(context, args);
        if (!document.TryGetValue("display", out var displayValue) ||
            !displayValue.IsString ||
            !document.TryGetValue("key", out var keyValue) ||
            !keyValue.IsBsonBinaryData)
        {
            throw new InvalidEventSequenceIdentityKey();
        }

        var identity = EventSequenceMutationIdentity.TryCreate(displayValue.AsString);
        var persistedKey = new EventSequenceIdentityKey(keyValue.AsBsonBinaryData.Bytes);
        if (!identity.IsSuccess || identity.Identity!.Key != persistedKey)
        {
            throw new InvalidEventSequenceIdentityKey();
        }

        return identity.Identity;
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EventSequenceMutationIdentity value)
    {
        var recreated = EventSequenceMutationIdentity.TryCreate(value.Display);
        if (!recreated.IsSuccess || recreated.Identity!.Key != value.Key)
        {
            throw new InvalidEventSequenceIdentityKey();
        }

        BsonDocumentSerializer.Instance.Serialize(
            context,
            args,
            new BsonDocument
            {
                ["display"] = value.Display,
                ["key"] = new BsonBinaryData(value.Key.Snapshot(), BsonBinarySubType.Binary)
            });
    }
}
