// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;
using Orleans.Runtime;

namespace Cratis.Kernel.Storage.MongoDB.Grains;

/// <summary>
/// Represents a <see cref="IBsonSerializer{T}"/> for handling serialization of <see cref="GrainId"/>.
/// </summary>
public class GrainIdSerializer : IBsonSerializer<GrainId>
{
    /// <inheritdoc/>
    public Type ValueType => typeof(GrainId);

    /// <inheritdoc/>
    public GrainId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => GrainId.Parse(context.Reader.ReadString());

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GrainId value) => context.Writer.WriteString(value.ToString());

    /// <inheritdoc/>
    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => Serialize(context, args, (GrainId)value);

    /// <inheritdoc/>
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => Deserialize(context, args);
}
