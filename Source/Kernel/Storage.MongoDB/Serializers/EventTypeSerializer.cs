// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson.Serialization;

<<<<<<<< HEAD:Source/Kernel/Storage.MongoDB/Serialization/EventTypeSerializer.cs
namespace Cratis.Chronicle.Storage.MongoDB.Serialization;
========
namespace Cratis.Chronicle.Storage.MongoDB.Serializers;
>>>>>>>> main:Source/Kernel/Storage.MongoDB/Serializers/EventTypeSerializer.cs

/// <summary>
/// Represents a BSON serializer for <see cref="EventType"/>.
/// </summary>
public class EventTypeSerializer : IBsonSerializer<EventType>
{
    /// <inheritdoc/>
    public Type ValueType => typeof(EventType);

    /// <inheritdoc/>
    public EventType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => EventType.Parse(context.Reader.ReadString());

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EventType value) => context.Writer.WriteString(value.ToString());

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (EventType)value!);

    /// <inheritdoc/>
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args)!;
}
