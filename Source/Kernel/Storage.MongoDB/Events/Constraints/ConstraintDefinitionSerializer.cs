// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a serializer for <see cref="IConstraintDefinition"/>.
/// </summary>
public class ConstraintDefinitionSerializer : SerializerBase<IConstraintDefinition>, IBsonDocumentSerializer
{
    const string ConstraintTypeElementName = "constraintType";

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IConstraintDefinition value)
    {
        var type = value.GetType();
        var actualSerializer = BsonSerializer.SerializerRegistry.GetSerializer(type);
        var document = value.ToBsonDocument(type, actualSerializer);
        document.Remove("_t");
        document.Add(ConstraintTypeElementName, GetConstraintTypeAsString(type));
        using var rawDocument = new ByteArrayBuffer(document.ToBson());
        context.Writer.WriteRawBsonDocument(rawDocument);
    }

    /// <inheritdoc/>
    public override IConstraintDefinition Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var rawBsonDocument = context.Reader.ReadRawBsonDocument();
        using var rawDocument = new RawBsonDocument(rawBsonDocument);
        var bsonDocument = rawDocument.ToBsonDocument<BsonDocument>();
        var constraintTypeString = bsonDocument.GetValue(ConstraintTypeElementName).AsString;
        var constraintType = (ConstraintType)Enum.Parse(typeof(ConstraintType), constraintTypeString);

        var type = constraintType switch
        {
            ConstraintType.Unique => typeof(UniqueConstraintDefinition),
            ConstraintType.UniqueEventType => typeof(UniqueEventTypeConstraintDefinition),
            _ => throw new UnknownConstraintTypeString(constraintTypeString)
        };

        using var reader = new BsonDocumentReader(bsonDocument);
        var ctx = BsonDeserializationContext.CreateRoot(reader);
        return (IConstraintDefinition)BsonSerializer.Deserialize(bsonDocument, type);
    }

    /// <inheritdoc/>
    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        serializationInfo = null!;
        return false;
    }

    string GetConstraintTypeAsString(Type type)
    {
        if (type == typeof(UniqueConstraintDefinition)) return nameof(ConstraintType.Unique);
        if (type == typeof(UniqueEventTypeConstraintDefinition)) return nameof(ConstraintType.UniqueEventType);

        throw new UnknownConstraintType(type);
    }
}
