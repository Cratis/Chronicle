// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Defines a converter that can convert between a <see cref="BsonDocument"/> and a <see cref="ExpandoObject"/> with a <see cref="JsonSchema"/> holding the type information.
/// </summary>
public interface IExpandoObjectConverter
{
    /// <summary>
    /// Convert a <see cref="BsonDocument"/> to <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="document"><see cref="BsonDocument"/> to convert.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> with the type information.</param>
    /// <returns>A new <see cref="ExpandoObject"/> instance.</returns>
    ExpandoObject ToExpandoObject(BsonDocument document, JsonSchema schema);

    /// <summary>
    /// Convert a <see cref="ExpandoObject"/> to <see cref="BsonDocument"/>.
    /// </summary>
    /// <param name="expandoObject">The <see cref="ExpandoObject"/> to convert.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> with the type information.</param>
    /// <returns>A new <see cref="BsonDocument"/> instance.</returns>
    BsonDocument ToBsonDocument(ExpandoObject expandoObject, JsonSchema schema);
}
