// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the equality comparer for <see cref="BsonDocumentArrayFilterDefinition{TDocument}"/>.
/// </summary>
public class ArrayFiltersEqualityComparer : EqualityComparer<BsonDocumentArrayFilterDefinition<BsonDocument>>
{
    /// <inheritdoc/>
    public override bool Equals(
        BsonDocumentArrayFilterDefinition<BsonDocument>? x,
        BsonDocumentArrayFilterDefinition<BsonDocument>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Document.Equals(y.Document);
    }

    /// <inheritdoc/>
    public override int GetHashCode(BsonDocumentArrayFilterDefinition<BsonDocument> obj) => obj.Document.GetHashCode();
}