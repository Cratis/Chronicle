// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// A parent a join unsets, in the documents it is null in, before the joined properties are written beneath it.
/// </summary>
/// <param name="Path">The mapped MongoDB path to the parent.</param>
/// <param name="Filters">The element filters positioning the parent, empty when it is not inside an array.</param>
internal record NullParentRepair(string Path, IReadOnlyList<BsonDocumentArrayFilterDefinition<BsonDocument>> Filters)
{
    /// <summary>
    /// Build the filter matching the documents the parent is null in.
    /// </summary>
    /// <returns>The <see cref="FilterDefinition{TDocument}"/>.</returns>
    public FilterDefinition<BsonDocument> ToNullFilter() =>
        Filters.Count == 0
            ? Builders<BsonDocument>.Filter.Type(Path, BsonType.Null)
            : ToNullArrayParentFilter();

    FilterDefinition<BsonDocument> ToNullArrayParentFilter()
    {
        // Reuse the element identifiers and null type check from the positional update filters,
        // but nest them inside $elemMatch so they apply to the same element in each array.
        var segments = Path.Split('.');
        BsonDocument? elementMatch = null;
        var childMarkerIndex = segments.Length;
        for (var index = Filters.Count - 1; index >= 0; index--)
        {
            var filter = Filters[index].Document;
            var identifier = filter.GetElement(0).Name.Split('.')[0];
            var markerIndex = Array.IndexOf(segments, $"$[{identifier}]");
            var predicate = ToPredicate(filter, identifier);

            if (elementMatch is not null)
            {
                var childPath = string.Join('.', segments.Skip(markerIndex + 1).Take(childMarkerIndex - markerIndex - 1));
                predicate[childPath] = new BsonDocument("$elemMatch", elementMatch);
            }

            elementMatch = predicate;
            childMarkerIndex = markerIndex;
        }

        var arrayPath = string.Join('.', segments.Take(childMarkerIndex));
        return new BsonDocument(arrayPath, new BsonDocument("$elemMatch", elementMatch));
    }

    static BsonDocument ToPredicate(BsonDocument filter, string identifier)
    {
        var predicate = new BsonDocument();
        foreach (var field in filter.Elements)
        {
            predicate.Add(field.Name[(identifier.Length + 1)..], field.Value);
        }

        return predicate;
    }
}
