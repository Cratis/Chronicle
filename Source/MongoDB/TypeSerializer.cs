// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.MongoDB;

/// <summary>
/// Represents a serializer for <see cref="Type"/>.
/// </summary>
public class TypeSerializer : SerializerBase<Type>
{
    /// <inheritdoc/>
    public override Type Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();
        switch (bsonType)
        {
            case BsonType.String:
                var type = context.Reader.ReadString();
                return Type.GetType(type) ?? throw new UnknownType(type);
        }

        throw new UnknownType(bsonType.ToString());
    }

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Type value)
    {
        context.Writer.WriteString(value.GetTypeString());
    }
}
