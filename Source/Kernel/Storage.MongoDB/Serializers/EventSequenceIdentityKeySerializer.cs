// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers;

/// <summary>
/// Represents a BSON serializer for <see cref="EventSequenceIdentityKey"/>.
/// </summary>
public class EventSequenceIdentityKeySerializer : SerializerBase<EventSequenceIdentityKey>
{
    /// <inheritdoc/>
    public override EventSequenceIdentityKey Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        new(context.Reader.ReadBinaryData().Bytes);

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EventSequenceIdentityKey value)
    {
        if (!value.IsInitialized)
        {
            throw new InvalidEventSequenceIdentityKey();
        }

        context.Writer.WriteBinaryData(new(value.Snapshot(), BsonBinarySubType.Binary));
    }
}
