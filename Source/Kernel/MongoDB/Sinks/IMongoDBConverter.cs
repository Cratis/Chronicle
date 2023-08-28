// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Properties;
using MongoDB.Bson;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Defines a system that can convert to MongoDB from properties and values.
/// </summary>
public interface IMongoDBConverter
{
    /// <summary>
    /// Convert a <see cref="PropertyPath"/> and <see cref="IArrayIndexers"/> to a MongoDB property.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to convert from.</param>
    /// <param name="arrayIndexers">Accompanying <see cref="IArrayIndexers"/> for indexing an array.</param>
    /// <returns>Converted <see cref="MongoDBProperty"/>.</returns>
    MongoDBProperty ToMongoDBProperty(PropertyPath propertyPath, IArrayIndexers arrayIndexers);

    /// <summary>
    /// Convert a <see cref="Key"/> to a <see cref="BsonValue"/>.
    /// </summary>
    /// <param name="key"><see cref="Key"/> to convert from.</param>
    /// <returns>Converted <see cref="BsonValue"/>.</returns>
    BsonValue ToBsonValue(Key key);

    /// <summary>
    /// Convert an object to a <see cref="BsonValue"/>.
    /// </summary>
    /// <param name="input">Input to convert.</param>
    /// <returns>Converted <see cref="BsonValue"/>.</returns>
    BsonValue ToBsonValue(object input);

    /// <summary>
    /// Convert an value within an object to a <see cref="BsonValue"/> based on its <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="input">Input object that holds the property.</param>
    /// <param name="property">The property to convert.</param>
    /// <returns>Converted <see cref="BsonValue"/>.</returns>
    BsonValue ToBsonValue(object? input, PropertyPath property);

    /// <summary>
    /// Convert an value within an object to a <see cref="BsonValue"/> based on its <see cref="JsonSchemaProperty"/>.
    /// </summary>
    /// <param name="input">Input object that holds the property.</param>
    /// <param name="schemaProperty">The property to convert.</param>
    /// <returns>Converted <see cref="BsonValue"/>.</returns>
    BsonValue ToBsonValue(object? input, JsonSchemaProperty schemaProperty);
}
