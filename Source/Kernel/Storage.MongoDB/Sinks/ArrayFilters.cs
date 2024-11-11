// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a set of array filters for the MongoDb queries.
/// </summary>
public class ArrayFilters : IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>>
{
    readonly HashSet<BsonDocumentArrayFilterDefinition<BsonDocument>> _filters = new(new ArrayFiltersEqualityComparer());

    /// <summary>
    /// Adds a single <see cref="BsonDocumentArrayFilterDefinition{T}"/> filter.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    public void Add(BsonDocumentArrayFilterDefinition<BsonDocument> filter) => _filters.Add(filter);

    /// <summary>
    /// Adds multiple <see cref="BsonDocumentArrayFilterDefinition{T}"/> filters.
    /// </summary>
    /// <param name="filters">The filters to add.</param>
    public void AddRange(IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>> filters)
    {
        foreach (var filter in filters)
        {
            Add(filter);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<BsonDocumentArrayFilterDefinition<BsonDocument>> GetEnumerator() => _filters.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}