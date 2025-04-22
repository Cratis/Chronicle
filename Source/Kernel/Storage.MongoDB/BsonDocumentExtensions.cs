// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="BsonDocument"/>.
/// </summary>
public static class BsonDocumentExtensions
{
    /// <summary>
    /// Remove CLR type information from a <see cref="BsonDocument"/>.
    /// </summary>
    /// <param name="document"><see cref="BsonDocument"/> to remove from.</param>
    public static void RemoveTypeInfo(this BsonDocument document)
    {
        var elementsToRemove = new List<string>();

        foreach (var child in document.Where(_ => _.Value is BsonDocument))
        {
            RemoveTypeInfo(child.Value.AsBsonDocument);
        }

        var hasDiscriminator = document.Any(_ => _.Name == "_t");
        if (hasDiscriminator)
        {
            document.Remove("_t");
        }
    }
}
