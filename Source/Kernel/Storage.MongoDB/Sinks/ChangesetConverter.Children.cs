// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;
using ChildrenSortDirection = Cratis.Chronicle.Changes.SortDirection;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class ChangesetConverter
{
    static void ApplyChildPush(UpdateAccumulator updates, string property, BsonValue bsonValue, ChildrenOrdering? ordering, PropertyPath identifiedBy)
    {
        // The default append keeps the plain $push it has always been.
        if (ordering?.IsAppend() != false)
        {
            updates.Push(property, bsonValue);
            return;
        }

        // Prepend: $push with $each and $position 0. Sorted: $push with $each and $sort, ordering the whole array the
        // same way the engine does - MongoDB orders missing and null values first ascending and last descending, and
        // equal sort values are ordered by the child's identifier ascending so the order is total and stable.
        int? position = ordering.Placement == ChildrenPlacement.Prepend ? 0 : null;
        var sort = ordering.Placement == ChildrenPlacement.Sorted
            ? new BsonDocumentSortDefinition<BsonValue>(SortDocumentFor(ordering, identifiedBy))
            : null;

        updates.PushEach(property, bsonValue, position, sort);
    }

    static BsonDocument SortDocumentFor(ChildrenOrdering ordering, PropertyPath identifiedBy)
    {
        var sortBy = ordering.SortBy.ToMongoDB();
        var sort = new BsonDocument(sortBy, ordering.Direction == ChildrenSortDirection.Descending ? -1 : 1);
        if (identifiedBy.IsSet && !identifiedBy.IsRoot)
        {
            var identifier = identifiedBy.ToMongoDB();
            if (identifier != sortBy)
            {
                sort.Add(identifier, 1);
            }
        }

        return sort;
    }

    void ApplyChildAdded(UpdateAccumulator updates, ChildAdded childAdded)
    {
        var bsonValue = ToChildBsonValue(childAdded.State, childAdded.ChildrenProperty);

        var property = ToChildCollectionProperty(childAdded.ChildrenProperty, childAdded.ArrayIndexers);
        updates.AddArrayFilters(property);

        ApplyChildPush(updates, property.Property, bsonValue, childAdded.Ordering, childAdded.IdentifiedByProperty);
    }

    MongoDBProperty ToChildCollectionProperty(PropertyPath childrenProperty, ArrayIndexers arrayIndexers)
    {
        var indexers = arrayIndexers.All.ToArray();
        var childIndex = Array.FindLastIndex(indexers, _ => _.ArrayProperty == childrenProperty);
        var parentIndexers = new ArrayIndexers(indexers.Where((_, index) => index != childIndex).ToArray());
        if (!parentIndexers.HasFor(childrenProperty))
        {
            return converter.ToMongoDBProperty(childrenProperty.GetChildrenProperty(), parentIndexers);
        }

        // Recursive parents share the child's normalized path; retain their selectors before the target collection.
        var parent = converter.ToMongoDBProperty(childrenProperty, parentIndexers);
        return new($"{parent.Property}.{childrenProperty.LastSegment.Value.ToMongoDBPropertyName()}", parent.ArrayFilters);
    }

    void ApplyNestedCleared(UpdateAccumulator updates, NestedCleared nestedCleared)
    {
        var property = converter.ToMongoDBProperty(nestedCleared.NestedProperty, nestedCleared.ArrayIndexers);
        updates.AddArrayFilters(property);
        updates.Unset(property.Property);
    }

    void ApplyChildRemoved(Key key, UpdateAccumulator updates, ChildRemoved childRemoved)
    {
        var property = ToChildCollectionProperty(childRemoved.ChildrenProperty, key.ArrayIndexers);
        updates.AddArrayFilters(property);

        var isPrimitive = ChildStateTypes.IsPrimitive(childRemoved.State);
        var identifiedByProperty = childRemoved.IdentifiedByProperty.ToMongoDB();

        if (!isPrimitive && !string.IsNullOrEmpty(identifiedByProperty))
        {
            // Pull an object child by its identifier rather than by whole-document equality. A child that
            // carries a [PII] member is stored with that member encrypted at rest, so a full-document match
            // against the plaintext removal state never matches and nothing is pulled — the in-memory sink
            // removes by key, so the two sinks would otherwise silently diverge. The identifier is not PII.
            //
            // The value is used unconverted, and unlike the join filter that is not a latent type mismatch. The
            // key here is the array indexer's identifier, which ResolveKey has already coerced to the type the
            // child's identified-by property declares (EnsureCorrectTypeForArrayIndexersOnKey, against the same
            // ITypeFormats this converter would use), and RemoveChild only raises a ChildRemoved once that value
            // has matched the child in the materialized state. Converting again would produce the same BSON, and
            // the unconverted call cannot throw because it is given no target type to coerce to.
            updates.PullFilter(property.Property, Builders<BsonDocument>.Filter.Eq(identifiedByProperty, childRemoved.Key.ToBsonValue()));
            return;
        }

        updates.Pull(property.Property, ToChildBsonValue(childRemoved.State, childRemoved.ChildrenProperty));
    }

    BsonValue ToChildBsonValue(object state, PropertyPath childrenProperty)
    {
        if (ChildStateTypes.IsPrimitive(state))
        {
            return state.ToBsonValue();
        }

        var schema = readModel.GetSchemaForLatestGeneration().GetSchemaForPropertyPath(childrenProperty);
        return expandoObjectConverter.ToBsonDocument((state as ExpandoObject)!, schema);
    }
}
