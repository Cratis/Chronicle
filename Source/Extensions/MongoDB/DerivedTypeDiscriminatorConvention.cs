// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents a MongoDB discriminator convention for handling types that have <see cref="DerivedTypeAttribute"/>.
/// </summary>
public class DerivedTypeDiscriminatorConvention : IDiscriminatorConvention
{
    public const string PropertyName = "_derivedTypeId";
    readonly IDerivedTypes _derivedTypes;

    public string ElementName => PropertyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DerivedTypeDiscriminatorConvention"/> class.
    /// </summary>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> in the system.</param>
    public DerivedTypeDiscriminatorConvention(IDerivedTypes derivedTypes)
    {
        _derivedTypes = derivedTypes;
    }

    /// <inheritdoc/>
    public Type GetActualType(IBsonReader bsonReader, Type nominalType)
    {
        var bookmark = bsonReader.GetBookmark();
        bsonReader.ReadStartDocument();

        var type = string.Empty;
        if (bsonReader.FindElement(ElementName))
        {
            type = bsonReader.ReadString();
        }

        bsonReader.ReturnToBookmark(bookmark);
        return _derivedTypes.GetDerivedTypeFor(nominalType, type);
    }

    /// <inheritdoc/>
    public BsonValue GetDiscriminator(Type nominalType, Type actualType)
    {
        var attribute = actualType.GetCustomAttribute<DerivedTypeAttribute>()!;
        return attribute.Identifier.ToString();
    }
}
