// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a discriminator convention for constraint types.
/// </summary>
public class ConstraintTypeDiscriminatorConvention : IDiscriminatorConvention
{
    /// <inheritdoc/>
    public string ElementName => "constraintType";

    /// <inheritdoc/>
    public Type GetActualType(IBsonReader bsonReader, Type nominalType)
    {
        var constraintType = ConstraintType.Unknown;
        var constraintTypeString = string.Empty;
        var bookmark = bsonReader.GetBookmark();
        bsonReader.ReadStartDocument();
        if (bsonReader.FindElement(ElementName))
        {
            constraintTypeString = bsonReader.ReadString();
            constraintType = (ConstraintType)Enum.Parse(typeof(ConstraintType), constraintTypeString);
        }

        bsonReader.ReturnToBookmark(bookmark);
        return constraintType switch
        {
            ConstraintType.Unique => typeof(UniqueEventTypeConstraintDefinition),
            ConstraintType.UniqueEventType => typeof(UniqueEventTypeConstraintDefinition),
            _ => throw new UnknownConstraintTypeString(constraintTypeString)
        };
    }

    /// <inheritdoc/>
    public BsonValue GetDiscriminator(Type nominalType, Type actualType)
    {
        if( actualType == typeof(UniqueConstraintDefinition) ) return new BsonString(nameof(ConstraintType.Unique));
        if( actualType == typeof(UniqueEventTypeConstraintDefinition) ) return new BsonString(nameof(ConstraintType.UniqueEventType));

        throw new UnknownConstraintType(actualType)
    }
}
