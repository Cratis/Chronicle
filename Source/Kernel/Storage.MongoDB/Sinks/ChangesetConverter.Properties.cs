// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class ChangesetConverter
{
    bool ApplyPropertiesChanged(
        UpdateAccumulator updates,
        ISet<PropertyPath> collectionPathsWithChildOperations,
        ISet<PropertyPath> wholeCollectionReplacementPaths,
        PropertiesChanged<ExpandoObject> propertiesChanged,
        ExpandoObject? initialState)
    {
        // Sink-owned system properties (e.g. __lastHandledEventSequenceNumber) are written via a
        // dedicated $max operator in AddLastHandledEventSequenceNumber. Including them in the
        // generic $set path here produces two operators targeting the same field, which MongoDB
        // rejects with "Updating the path 'X' would create a conflict at 'X'". WithoutCollectionConflicts
        // applies the same rule to element-level differences that collide with a child operation or a
        // whole-collection replacement on the same collection.
        var applicableDifferences = propertiesChanged.Differences
            .Where(_ => !_.PropertyPath.IsMongoDBKey() && !_.PropertyPath.IsSinkOwnedSystemProperty())
            .WithoutCollectionConflicts(collectionPathsWithChildOperations, wholeCollectionReplacementPaths)
            .ToArray();

        foreach (var propertyDifference in applicableDifferences)
        {
            if (propertyDifference.IsRemoval)
            {
                ApplyRemoval(updates, propertyDifference);
                continue;
            }

            AddMissingParents(updates, initialState, propertyDifference);
            ApplySet(updates, propertyDifference);
        }

        // Report hasChanges based on the original diff set, not the filtered one: a diff that
        // contained only filtered properties (e.g. _id or __lastHandledEventSequenceNumber) is
        // still semantically a change as far as the read-modify-write cycle is concerned, and
        // AddLastHandledEventSequenceNumber needs to fire to advance the sequence number.
        return propertiesChanged.Differences.Any();
    }

    void ApplyRemoval(UpdateAccumulator updates, PropertyDifference propertyDifference)
    {
        // A removal takes the key away entirely. $unset of a field - or of a field under a parent - that does not
        // exist is a no-op in MongoDB, so no missing parents need to be created first.
        var removed = converter.ToMongoDBProperty(propertyDifference.PropertyPath, propertyDifference.ArrayIndexers);
        updates.AddArrayFilters(removed);
        updates.Unset(removed.Property);
    }

    void AddMissingParents(UpdateAccumulator updates, ExpandoObject? initialState, PropertyDifference propertyDifference)
    {
        foreach (var parent in MissingParents.Find(initialState, propertyDifference.PropertyPath, propertyDifference.ArrayIndexers))
        {
            updates.AddNullParent(converter.ToMongoDBProperty(parent, propertyDifference.ArrayIndexers));
        }
    }

    void ApplySet(UpdateAccumulator updates, PropertyDifference propertyDifference)
    {
        var property = converter.ToMongoDBProperty(propertyDifference.PropertyPath, propertyDifference.ArrayIndexers);
        updates.AddArrayFilters(property);
        var value = converter.ToBsonValue(propertyDifference.Changed, propertyDifference.PropertyPath);
        updates.Set(property.Property, value);
    }
}
