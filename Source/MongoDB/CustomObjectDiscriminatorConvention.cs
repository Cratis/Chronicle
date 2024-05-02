// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.MongoDB;

/// <summary>
/// Represents a custom <see cref="IDiscriminatorConvention"/> for handling object properties.
/// </summary>
public class CustomObjectDiscriminatorConvention : IDiscriminatorConvention
{
    /// <summary>
    /// Singleton instance of <see cref="CustomObjectDiscriminatorConvention"/>.
    /// </summary>
    internal static readonly CustomObjectDiscriminatorConvention Instance = new();
    readonly ObjectDiscriminatorConvention _convention;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomObjectDiscriminatorConvention"/> class.
    /// </summary>
    public CustomObjectDiscriminatorConvention()
    {
        _convention = new("_t");
    }

    /// <inheritdoc/>
    public string ElementName => _convention.ElementName;

    /// <inheritdoc/>
    public Type GetActualType(IBsonReader bsonReader, Type nominalType)
    {
        Type? actualType = null;
        var bookmark = bsonReader.GetBookmark();
        bsonReader.ReadStartDocument();
        if (bsonReader.FindElement(_convention.ElementName))
        {
            var context = BsonDeserializationContext.CreateRoot(bsonReader);
            var discriminator = BsonValueSerializer.Instance.Deserialize(context);
            if (discriminator is BsonString discriminatorString)
            {
                actualType = Type.GetType(discriminatorString.Value);
            }
        }

        bsonReader.ReturnToBookmark(bookmark);
        return actualType ?? _convention.GetActualType(bsonReader, nominalType);
    }

    /// <inheritdoc/>
    public BsonValue GetDiscriminator(Type nominalType, Type actualType) => actualType.GetTypeString();
}
