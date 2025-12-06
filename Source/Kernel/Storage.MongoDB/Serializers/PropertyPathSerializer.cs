// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Serializers;

/// <summary>
/// Represents a serializer for <see cref="PropertyPath"/> to and from BSON.
/// </summary>
public class PropertyPathSerializer : SerializerBase<PropertyPath>
{
    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, PropertyPath value)
    {
        context.Writer.WriteString(value.Path);
    }

    /// <inheritdoc/>
    public override PropertyPath Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new PropertyPath(context.Reader.ReadString());
    }
}
