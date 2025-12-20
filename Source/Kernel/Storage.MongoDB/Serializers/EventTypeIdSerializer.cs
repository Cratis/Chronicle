// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson.Serialization;

<<<<<<<< HEAD:Source/Kernel/Storage.MongoDB/Serialization/EventTypeIdSerializer.cs
namespace Cratis.Chronicle.Storage.MongoDB.Serialization;
========
namespace Cratis.Chronicle.Storage.MongoDB.Serializers;
>>>>>>>> main:Source/Kernel/Storage.MongoDB/Serializers/EventTypeIdSerializer.cs

/// <summary>
/// Represents a BSON serializer for <see cref="EventTypeId"/>.
/// </summary>
public class EventTypeIdSerializer : IBsonSerializer<EventTypeId>
{
    /// <inheritdoc/>
    public Type ValueType => typeof(EventTypeId);

    /// <inheritdoc/>
    public EventTypeId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => context.Reader.ReadString();

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EventTypeId value) => context.Writer.WriteString(value.ToString());

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (EventTypeId)value!);

    /// <inheritdoc/>
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args)!;
}
