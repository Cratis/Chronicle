// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="BsonDocument"/>.
/// </summary>
public static class BsonDocumentExtensions
{
    /// <summary>
    /// Remove CLR type information from a <see cref="BsonDocument"/>.
    /// </summary>
    /// <param name="document"><see cref="BsonDocument"/> to remove from.</param>
    public static void RemoveTypeInfo(this BsonDocument document) => RemoveTypeInfoImplementation(document);

    static bool RemoveTypeInfoImplementation(this BsonDocument document, BsonDocument? parent = null, string? childProperty = null)
    {
        var elementsToRemove = new List<string>();

        foreach (var child in document.Where(_ => _.Value is BsonDocument))
        {
            if (RemoveTypeInfoImplementation(child.Value.AsBsonDocument, document, child.Name))
            {
                elementsToRemove.Add(child.Name);
            }
        }

        foreach (var elementToRemove in elementsToRemove)
        {
            document.Remove(elementToRemove);
        }

        if (!string.IsNullOrEmpty(childProperty) && parent is not null)
        {
            return document.Any(_ => _.Name == "_t");
        }

        return false;
    }
}
