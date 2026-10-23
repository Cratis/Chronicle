// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The boundary for direct multi-document data mutations, which cannot identify all affected cached keys.
/// </summary>
/// <param name="collections">The sink's current collection context.</param>
/// <param name="invalidatePrefetch">The owning sink's optional prefetch invalidation.</param>
internal sealed class SinkMultiDocumentWrites(ISinkCollections collections, Action? invalidatePrefetch = null)
{
    /// <summary>Invalidates read-only prefetch state before a multi-document operation or its join drain.</summary>
    internal void BeforeWrite() => invalidatePrefetch?.Invoke();

    /// <summary>Invalidates prefetch before every direct multi-document database mutation.</summary>
    /// <param name="filter">The documents to update.</param>
    /// <param name="update">The update to apply.</param>
    /// <param name="options">The update options.</param>
    /// <returns>The database update result.</returns>
    internal Task<UpdateResult> UpdateManyAsync(FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options)
    {
        BeforeWrite();
        return collections.GetCollection().UpdateManyAsync(filter, update, options);
    }
}
