// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static Cratis.Chronicle.Storage.Sql.Sinks.JsonColumnDocuments;

#pragma warning disable SA1202 // Private helpers grouped next to the methods they back.
#pragma warning disable SA1204 // Static helpers grouped next to the methods they back.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that applies projected changes to a tracked read model row.
/// </summary>
public partial class Sink
{
    void ApplyChangesToEntity(EntityEntry<DynamicReadModelEntity> entry, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    ApplyPropertyChanges(entry, propertiesChanged);
                    break;

                case ChildAdded childAdded:
                    ApplyChildAdded(entry, childAdded);
                    break;

                case ChildRemoved childRemoved:
                    ApplyChildRemoved(entry, childRemoved);
                    break;

                case NestedCleared nestedCleared:
                    ApplyNestedCleared(entry, nestedCleared);
                    break;

                case Joined joined:
                    ApplyChangesToEntity(entry, joined.Changes);
                    break;

                case ResolvedJoin resolvedJoin:
                    ApplyChangesToEntity(entry, resolvedJoin.Changes);
                    break;
            }
        }
    }

    void ApplyPropertyChanges(EntityEntry<DynamicReadModelEntity> entry, PropertiesChanged<ExpandoObject> propertiesChanged)
    {
        foreach (var difference in propertiesChanged.Differences)
        {
            ApplySingleDifference(entry, difference);
        }
    }

    void ApplySingleDifference(EntityEntry<DynamicReadModelEntity> entry, PropertyDifference difference)
    {
        var firstSegment = difference.PropertyPath.Segments.FirstOrDefault()?.Value;
        if (firstSegment is null || IsNonBooleanInitializedMarker(firstSegment, difference))
        {
            return;
        }

        var column = FindColumn(firstSegment);
        if (column is null)
        {
            // The PropertyPath has no top-level column segment — it is leaf-relative (e.g. "FloatValue").
            // When updating an existing child via SetProperties, property mappers produce leaf-only paths
            // paired with collection-locating ArrayIndexers (e.g. {Children[StringValue=SecondKey]}).
            // Fall back to locating the JSON column through the ArrayIndexers and applying the change
            // within the correct element, matching MongoDB $set semantics for nested array updates.
            TryApplyDifferenceViaArrayIndexers(entry, difference);
            return;
        }

        if (!column.IsJson)
        {
            if (difference.PropertyPath.Segments.Count() == 1)
            {
                SetTypedColumn(entry, column, difference.Changed);
            }

            return;
        }

        ApplyJsonColumnDifference(entry, column, difference);
    }

    static bool IsNonBooleanInitializedMarker(string firstSegment, PropertyDifference difference) =>
        firstSegment == WellKnownProperties.ReadModelInstanceInitialized && difference.Changed is not bool;

    void ApplyJsonColumnDifference(EntityEntry<DynamicReadModelEntity> entry, ProjectedColumn column, PropertyDifference difference)
    {
        var current = LoadJsonColumnState(entry, column);
        var indexers = difference.ArrayIndexers ?? ArrayIndexers.NoIndexers;
        var subPath = BuildSubPath(difference.PropertyPath);

        // BuildSubPath returns an empty PropertyPath when the difference targets the JSON column
        // itself (a single-segment path such as "PointValue"). An empty PropertyPath still yields a
        // single empty-string segment, so checking Segments.Any() would wrongly route the whole
        // value through ApplyDifferenceToJsonRoot and store it under an "" key (e.g.
        // {"":{"Longitude":..}}), which then materializes back as a default value. IsSet is false
        // for an empty path, so use it to detect "replace the entire column value".
        if (!subPath.IsSet)
        {
            entry.Entity[column.Name] = SerializeJsonColumn(UnwrapForJson(difference.Changed), column);
        }
        else
        {
            var rebuilt = ApplyDifferenceToJsonRoot(current, column, new JsonDifference(subPath, indexers, difference.Changed));
            entry.Entity[column.Name] = SerializeJsonColumn(rebuilt, column);
        }

        entry.Property(column.Name).IsModified = true;
    }

    void TryApplyDifferenceViaArrayIndexers(EntityEntry<DynamicReadModelEntity> entry, PropertyDifference difference)
    {
        var indexers = difference.ArrayIndexers;
        if (indexers.IsEmpty)
        {
            return;
        }

        var jsonColumn = indexers.All
            .Select(indexer => indexer.ArrayProperty.Segments.FirstOrDefault()?.Value)
            .Where(columnName => columnName is not null)
            .Select(columnName => _columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.Ordinal) && c.IsJson))
            .FirstOrDefault(column => column is not null);
        if (jsonColumn is null)
        {
            return;
        }

        // The difference's PropertyPath is already relative to the array element (e.g. "FloatValue").
        // Pass it directly as the subPath — ApplyDifferenceToJsonRoot will locate the element via
        // the ArrayIndexers and apply the leaf property change within it.
        var current = LoadJsonColumnState(entry, jsonColumn);
        var rebuilt = ApplyDifferenceToJsonRoot(current, jsonColumn, new JsonDifference(difference.PropertyPath, indexers, difference.Changed));
        entry.Entity[jsonColumn.Name] = SerializeJsonColumn(rebuilt, jsonColumn);
        entry.Property(jsonColumn.Name).IsModified = true;
    }

    void ApplyChildAdded(EntityEntry<DynamicReadModelEntity> entry, ChildAdded childAdded)
    {
        var pathSegments = childAdded.ChildrenProperty.Segments.ToArray();
        if (pathSegments.Length == 0)
        {
            return;
        }

        var column = FindColumn(pathSegments[0].Value);
        if (column?.IsJson != true)
        {
            return;
        }

        var current = LoadJsonColumnState(entry, column);
        var unwrappedState = UnwrapForJson(childAdded.State);

        var identifierName = childAdded.IdentifiedByProperty;
        var identifierValue = UnwrapConceptValue(childAdded.Key);
        var list = current as IList<object?> ?? new List<object?>();
        if (pathSegments.Length == 1)
        {
            // Top-level children collection on the column: upsert by identifier so repeated
            // ChildAdded events for the same identifier (e.g. during replay) update in place
            // instead of duplicating. Mirrors how MongoDB's projection engine treats a child
            // re-introduced via ChildAdded as a replacement when the identifier already exists.
            UpsertChildInList(list, identifierName, identifierValue, unwrappedState);
        }
        else
        {
            // Nested children — e.g. Configurations.Hubs — locate the matching parent inside the
            // column's JSON tree using ArrayIndexers, then upsert into its child collection.
            var indexers = childAdded.ArrayIndexers ?? ArrayIndexers.NoIndexers;
            UpsertNestedChild(list, 1, new ChildUpsert(pathSegments, indexers, identifierName, identifierValue, unwrappedState));
        }

        entry.Entity[column.Name] = SerializeJsonColumn(list, column);
        entry.Property(column.Name).IsModified = true;
    }

    void ApplyChildRemoved(EntityEntry<DynamicReadModelEntity> entry, ChildRemoved childRemoved)
    {
        var column = FindColumn(childRemoved.ChildrenProperty.Segments.First().Value);
        if (column?.IsJson != true)
        {
            return;
        }

        if (LoadJsonColumnState(entry, column) is not IList<object?> current)
        {
            return;
        }

        var identifier = childRemoved.IdentifiedByProperty;
        var keyAsString = childRemoved.Key?.ToString();
        var retained = current.Where(item => !IsRemovedChild(item, identifier, childRemoved.Key, keyAsString)).ToList();

        entry.Entity[column.Name] = SerializeJsonColumn(retained, column);
        entry.Property(column.Name).IsModified = true;
    }

    void ApplyNestedCleared(EntityEntry<DynamicReadModelEntity> entry, NestedCleared nestedCleared)
    {
        var pathSegments = nestedCleared.NestedProperty.Segments.ToArray();
        if (pathSegments.Length == 0)
        {
            return;
        }

        var column = FindColumn(pathSegments[0].Value);
        if (column is null)
        {
            return;
        }

        if (pathSegments.Length == 1)
        {
            // Clearing the column itself — represent "no value" as JSON null so that downstream
            // deserialization sees a missing object rather than an empty record. NestedCleared is
            // explicit about wiping the nested object; preserving a JSON "{}" would resurrect it
            // as an empty instance and break tests that assert ShouldBeNull.
            entry.Entity[column.Name] = column.IsJson ? "null" : null;
            entry.Property(column.Name).IsModified = true;
            return;
        }

        if (column.IsJson)
        {
            ClearNestedInJsonColumn(entry, column, nestedCleared);
        }
    }

    void ClearNestedInJsonColumn(EntityEntry<DynamicReadModelEntity> entry, ProjectedColumn column, NestedCleared nestedCleared)
    {
        // Clear a sub-path within the JSON column — navigate to the parent, then null out the
        // final property. Mirrors the column-level case for nested-in-children scenarios.
        var current = LoadJsonColumnState(entry, column);
        var indexers = nestedCleared.ArrayIndexers ?? ArrayIndexers.NoIndexers;
        var subPath = BuildSubPath(nestedCleared.NestedProperty);
        if (!subPath.Segments.Any())
        {
            entry.Entity[column.Name] = column.IsArray ? "[]" : "null";
            entry.Property(column.Name).IsModified = true;
            return;
        }

        var rebuilt = ApplyDifferenceToJsonRoot(current, column, new JsonDifference(subPath, indexers, null));
        entry.Entity[column.Name] = SerializeJsonColumn(rebuilt, column);
        entry.Property(column.Name).IsModified = true;
    }

    void SeedDefaults(DynamicReadModelEntity entity)
    {
        foreach (var column in _columns.Where(column => !column.IsKey && !column.IsNullable))
        {
            if (TryGetDefaultValue(column, out var defaultValue))
            {
                entity[column.Name] = defaultValue;
            }
        }
    }

    static bool TryGetDefaultValue(ProjectedColumn column, out object? value)
    {
        value = null;
        if (column.Name == WellKnownProperties.ReadModelInstanceInitialized)
        {
            value = true;
            return true;
        }

        if (column.IsJson)
        {
            value = column.IsArray ? "[]" : "{}";
            return true;
        }

        if (column.ClrType == typeof(string))
        {
            value = string.Empty;
            return true;
        }

        if (!column.ClrType.IsValueType)
        {
            return false;
        }

        value = Activator.CreateInstance(column.ClrType);
        return true;
    }

    static void SetTypedColumn(EntityEntry<DynamicReadModelEntity> entry, ProjectedColumn column, object? value)
    {
        entry.Entity[column.Name] = CoerceForColumn(column, value);
        entry.Property(column.Name).IsModified = true;
    }

    /// <summary>
    /// Determines whether a guarded write would move the row's last handled event sequence number forward.
    /// </summary>
    /// <param name="entry">The <see cref="EntityEntry{TEntity}"/> for the read model row.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> about to be applied.</param>
    /// <returns>True when the write must be applied, false when it must be skipped.</returns>
    /// <remarks>
    /// A guarded write never creates the row, matching the MongoDB sink, where a conditional upsert would surface
    /// the already-applied case as a duplicate key error. A row that predates the watermark column carries no
    /// value, which the null check admits so it establishes one on its first guarded write.
    /// </remarks>
    bool AdvancesWatermark(EntityEntry<DynamicReadModelEntity> entry, EventSequenceNumber eventSequenceNumber)
    {
        if (entry.State == EntityState.Added)
        {
            return false;
        }

        var existing = ReadLastHandledSequenceNumber(entry);
        return existing is null || existing < (long)eventSequenceNumber.Value;
    }

    long? ReadLastHandledSequenceNumber(EntityEntry<DynamicReadModelEntity> entry)
    {
        var column = FindColumn(WellKnownProperties.LastHandledEventSequenceNumber);
        if (column is null || !entry.Entity.TryGetValue(column.Name, out var existingValue) || existingValue is null)
        {
            return null;
        }

        return Convert.ToInt64(existingValue, CultureInfo.InvariantCulture);
    }

    void AdvanceLastHandledSequenceNumber(EntityEntry<DynamicReadModelEntity> entry, EventSequenceNumber eventSequenceNumber)
    {
        // A sentinel is not a position in the sequence; storing one would pin the watermark at the top of the
        // range and permanently block every later guarded write to the row.
        if (!eventSequenceNumber.IsActualValue)
        {
            return;
        }

        var column = FindColumn(WellKnownProperties.LastHandledEventSequenceNumber);
        if (column is null)
        {
            return;
        }

        var incoming = (long)(ulong)eventSequenceNumber;
        var existing = ReadLastHandledSequenceNumber(entry) ?? 0L;
        var next = Math.Max(incoming, existing);
        if (next == existing && entry.State != EntityState.Added)
        {
            return;
        }

        entry.Entity[column.Name] = next;
        entry.Property(column.Name).IsModified = true;
    }

    object? LoadJsonColumnState(EntityEntry<DynamicReadModelEntity> entry, ProjectedColumn column)
    {
        if (!entry.Entity.TryGetValue(column.Name, out var current) || current is null)
        {
            return column.IsArray ? new List<object?>() : new ExpandoObject();
        }

        if (current is string jsonString)
        {
            return DeserializeJsonColumn(jsonString) ?? (column.IsArray ? new List<object?>() : new ExpandoObject());
        }

        return current;
    }

    static object ApplyDifferenceToJsonRoot(object? current, ProjectedColumn column, JsonDifference difference)
    {
        if (current is ExpandoObject rootExpando)
        {
            difference.SubPath.SetValue(rootExpando, difference.Changed!, difference.Indexers);
            return rootExpando;
        }

        if (current is IList<object?> list)
        {
            return ApplyDifferenceToList(list, column, difference);
        }

        var fresh = new ExpandoObject();
        difference.SubPath.SetValue(fresh, difference.Changed!, difference.Indexers);
        return fresh;
    }

    static IList<object?> ApplyDifferenceToList(IList<object?> list, ProjectedColumn column, JsonDifference difference)
    {
        // The JSON column is an array (e.g. Features). The change targets a specific element
        // selected by an ArrayIndexer whose ArrayProperty matches the column name; the subPath
        // is the property path WITHIN that element. EnsurePath cannot run here because it
        // navigates from an ExpandoObject root and would mis-classify the column name as a
        // PropertyName instead of an ArrayProperty — instead, locate the element ourselves
        // and apply the inner change directly to it. Sub-element indexers (deeper arrays inside
        // the located element, e.g. Slices inside Features) need their ArrayProperty rewritten
        // to drop the column prefix so EnsurePath can find them while walking the subPath.
        var indexer = difference.Indexers.All.FirstOrDefault(i => i.ArrayProperty.LastSegment.Value == column.Name);
        if (indexer is null)
        {
            return list;
        }

        var element = LocateOrCreateChild(list, indexer);
        if (!difference.SubPath.Segments.Any())
        {
            // The whole element is being replaced.
            ReplaceChildContents(element, difference.Changed);
        }
        else
        {
            difference.SubPath.SetValue(element, difference.Changed!, RewriteIndexersRelativeTo(column.Name, difference.Indexers));
        }

        return list;
    }

    sealed record JsonDifference(PropertyPath SubPath, ArrayIndexers Indexers, object? Changed);
}
