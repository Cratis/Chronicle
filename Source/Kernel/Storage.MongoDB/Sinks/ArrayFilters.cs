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
    readonly HashSet<string> _arrayPaths = [];

    /// <summary>
    /// Gets the top-level array paths the filtered properties are positioned in. MongoDB rejects an update whose array
    /// filters address a path the document does not have (error 2), so a write carrying these filters only applies to a
    /// document that holds every one of them.
    /// </summary>
    public IReadOnlyCollection<string> ArrayPaths => _arrayPaths;

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

    /// <summary>
    /// Adds the filters for a property and records the top-level array path the property is positioned in.
    /// </summary>
    /// <param name="property">The MongoDB property the filters position, such as applications.$[a].name.</param>
    /// <param name="filters">The filters to add.</param>
    public void AddRange(string property, IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>> filters)
    {
        var filtersForProperty = filters.ToArray();
        if (filtersForProperty.Length == 0)
        {
            return;
        }

        var markerIndex = property.IndexOf(".$[", StringComparison.Ordinal);
        if (markerIndex > 0)
        {
            _arrayPaths.Add(property[..markerIndex]);
        }

        AddRange(filtersForProperty);
    }

    /// <inheritdoc/>
    public IEnumerator<BsonDocumentArrayFilterDefinition<BsonDocument>> GetEnumerator() => _filters.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}