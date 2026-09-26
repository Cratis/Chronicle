// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IChangesetConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChangesetConverter"/> class.
/// </remarks>
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
/// <param name="converter"><see cref="IMongoDBConverter"/> to use.</param>
/// <param name="collections"><see cref="ISinkCollections"/> to use.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/> for logging.</param>
public class ChangesetConverter(
    ReadModelDefinition readModel,
    IMongoDBConverter converter,
    ISinkCollections collections,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ChangesetConverter> logger) : IChangesetConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangesetConverter"/> class without a logger.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
    /// <param name="converter"><see cref="IMongoDBConverter"/> to use.</param>
    /// <param name="collections"><see cref="ISinkCollections"/> to use.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <remarks>
    /// Retained so a caller written against the previous constructor keeps compiling; it forgoes the diagnostic
    /// naming a join that matched no documents.
    /// </remarks>
    public ChangesetConverter(
        ReadModelDefinition readModel,
        IMongoDBConverter converter,
        ISinkCollections collections,
        IExpandoObjectConverter expandoObjectConverter)
        : this(readModel, converter, collections, expandoObjectConverter, NullLogger<ChangesetConverter>.Instance)
    {
    }

    /// <inheritdoc/>
    public async Task<UpdateDefinitionAndArrayFilters> ToUpdateDefinition(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        var hasChanges = false;
        var updateDefinitionBuilder = Builders<BsonDocument>.Update;
        UpdateDefinition<BsonDocument>? updateBuilder = default;
        var normalizedChanges = NormalizeJoinedChanges(changeset.Changes);

        var arrayFiltersForDocument = new ArrayFilters();
        var nullParentPaths = new HashSet<string>();
        var nullArrayParents = new List<NullArrayParent>();
        await ApplyActualChanges(
            key,
            normalizedChanges,
            changeset.InitialState,
            nullParentPaths,
            nullArrayParents,
            updateDefinitionBuilder,
            ref updateBuilder,
            ref hasChanges,
            arrayFiltersForDocument,
            eventSequenceNumber);

        // EventSequenceNumber sentinels (Unavailable, Max) are not positions in the sequence. Letting one through
        // would pin the $max watermark at the top of the range and permanently block every later guarded write to
        // the document.
        if (hasChanges && eventSequenceNumber.IsActualValue)
        {
            BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref updateBuilder, eventSequenceNumber);
        }

        var distinctArrayFilters = arrayFiltersForDocument.DistinctBy(_ => _.Document).ToArray();

        return new(updateBuilder!, distinctArrayFilters, hasChanges)
        {
            NullParentPaths = nullParentPaths.OrderBy(_ => _.Count(ch => ch == '.')).ToArray(),
            NullArrayParents = nullArrayParents.DistinctBy(_ => (_.Path, string.Join('|', _.ArrayFilters.Select(filter => filter.Document.ToJson())))).ToArray()
        };
    }

    /// <summary>
    /// Build the property and comparand the <see cref="Joined"/> change filters its update on.
    /// </summary>
    /// <param name="key">The resolved <see cref="Key"/> the change is applied under.</param>
    /// <param name="joined">The <see cref="Joined"/> change to build the filter for.</param>
    /// <returns>The <see cref="JoinFilterTarget"/> to filter on.</returns>
    /// <remarks>
    /// Internal so the conversion matrix can be specified directly rather than only through a write.
    /// </remarks>
    internal JoinFilterTarget CreateJoinFilterTarget(Key key, Joined joined)
    {
        // When array indexers are present, the join key (key.Value) is the root document key — not
        // the child identifier. Use the last array indexer's property path and value to build a filter
        // that matches ALL root documents containing this child, so UpdateManyAsync updates every
        // document that has the child (e.g. all groups that contain the user), not just the first one
        // found during key resolution.
        if (!key.ArrayIndexers.IsEmpty)
        {
            var lastIndexer = key.ArrayIndexers.All.Last();
            var filterPath = lastIndexer.ArrayProperty + lastIndexer.IdentifierProperty;
            var (childProperty, _) = converter.ToMongoDBProperty(filterPath, ArrayIndexers.NoIndexers);
            return new(childProperty, ToFilterValue(lastIndexer.Identifier, filterPath));
        }

        // The join key is the join source's raw event source id — always a string. The joined-on column is
        // stored in whatever BSON representation its schema dictates, so a Guid-backed column holds
        // BinData and a string comparand matches nothing: the UpdateMany reports zero matched, which is
        // indistinguishable from a successful write. Convert through the schema, exactly as the child
        // branch above already does for its array-indexer identifier.
        var (property, _) = converter.ToMongoDBProperty(joined.OnProperty, ArrayIndexers.NoIndexers);
        return new(property, ToFilterValue(joined.Key, joined.OnProperty));
    }

    static bool TryMergeJoinedChangeIntoChildAdded(IList<Change> changes, Joined joined)
    {
        if (joined.ArrayIndexers.IsEmpty)
        {
            return false;
        }

        var targetIndexer = joined.ArrayIndexers.All.Last();
        var matchingChild = changes
            .OfType<ChildAdded>()
            .FirstOrDefault(_ =>
                _.ChildrenProperty == targetIndexer.ArrayProperty &&
                _.IdentifiedByProperty == targetIndexer.IdentifierProperty &&
                _.Key.ToString() == joined.Key.ToString());

        if (matchingChild?.Child is not ExpandoObject child)
        {
            return false;
        }

        ApplyJoinedChangesToChild(child, joined.Changes);
        return true;
    }

    static void ApplyJoinedChangesToChild(ExpandoObject child, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    foreach (var difference in propertiesChanged.Differences.Where(_ => !_.PropertyPath.IsMongoDBKey() && !_.PropertyPath.IsSinkOwnedSystemProperty()))
                    {
                        difference.PropertyPath.SetValue(child, difference.Changed!, ArrayIndexers.NoIndexers);
                    }

                    break;

                case ResolvedJoin resolvedJoin:
                    ApplyJoinedChangesToChild(child, resolvedJoin.Changes);
                    break;
            }
        }
    }

    static IEnumerable<PropertyPath> MissingParents(ExpandoObject? initialState, PropertyPath path, ArrayIndexers indexers)
    {
        // Identifier-less indexers cannot safely select a child for a pre-unset: their MongoDB
        // filter has no stable identity, and a numeric path can shift between reading and writing.
        // Leave their leaf update on the existing path rather than risk unsetting the wrong child.
        if (indexers.All.Any(_ => !_.IdentifierProperty.IsSet))
        {
            yield break;
        }

        var segments = path.Segments.ToArray();
        var current = initialState as IDictionary<string, object?>;
        var parentPath = PropertyPath.Root;
        var insideArray = false;
        for (var index = 0; index < segments.Length - 1; index++)
        {
            parentPath += segments[index];
            if (segments[index] is ArrayProperty)
            {
                // Only indexed elements can be repaired without changing their siblings.
                if (!indexers.HasFor(parentPath))
                {
                    yield break;
                }

                insideArray = true;
                current = null;
                continue;
            }

            if (insideArray || current is null || !current.TryGetValue(segments[index].Value, out var value) || value is null)
            {
                yield return parentPath;
                current = null;
            }
            else
            {
                current = value as IDictionary<string, object?>;
            }
        }
    }

    Task ApplyActualChanges(
        Key key,
        IEnumerable<Change> changes,
        ExpandoObject? initialState,
        ISet<string> nullParentPaths,
        IList<NullArrayParent> nullArrayParents,
        UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder,
        ref UpdateDefinition<BsonDocument>? updateBuilder,
        ref bool hasChanges,
        ArrayFilters arrayFiltersForDocument,
        EventSequenceNumber eventSequenceNumber)
    {
        var joinTasks = new List<Task>();
        var changesToApply = changes.ToList();
        var collectionPathsWithChildOperations = changesToApply.GetCollectionPathsWithChildOperations();
        var wholeCollectionReplacementPaths = changesToApply.GetWholeCollectionReplacementPaths();

        foreach (var change in changesToApply)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    hasChanges |= BuildPropertiesChanged(updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, collectionPathsWithChildOperations, wholeCollectionReplacementPaths, propertiesChanged, initialState, nullParentPaths, nullArrayParents);
                    break;

                case ChildAdded childAdded:
                    BuildChildAdded(updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, childAdded);
                    hasChanges = true;
                    break;

                case ChildRemoved childRemoved:
                    BuildChildRemoved(key, updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, childRemoved);
                    hasChanges = true;
                    break;

                case NestedCleared nestedCleared:
                    BuildNestedCleared(updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, nestedCleared);
                    hasChanges = true;
                    break;

                case Joined joined:
                    joinTasks.Add(PerformJoined(key, updateDefinitionBuilder, joined, eventSequenceNumber));
                    break;

                case ResolvedJoin resolvedJoined:
                    var applyActualChangesTask = ApplyActualChanges(key, resolvedJoined.Changes, initialState, nullParentPaths, nullArrayParents, updateDefinitionBuilder, ref updateBuilder, ref hasChanges, arrayFiltersForDocument, eventSequenceNumber);
                    joinTasks.Add(applyActualChangesTask);
                    break;
            }
        }

        return Task.WhenAll(joinTasks);
    }

    void BuildLastHandledEventSequenceNumber(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, EventSequenceNumber eventSequenceNumber)
    {
        var value = converter.ToBsonValue(eventSequenceNumber);

        // Use $max so this value can only ever move forward in MongoDB. When events for the
        // same read-model key are processed out of order — which happens when catch-up
        // dispatches per-partition steps for a constant-key or join projection — a plain $set
        // would let an earlier sequence number overwrite a later one, leaving the read model
        // pointing at a sequence number it has already moved past.
        if (updateBuilder != default)
        {
            updateBuilder = updateBuilder.Max(WellKnownProperties.LastHandledEventSequenceNumber, value);
        }
        else
        {
            updateBuilder = updateDefinitionBuilder.Max(WellKnownProperties.LastHandledEventSequenceNumber, value);
        }
    }

    bool BuildPropertiesChanged(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, ISet<PropertyPath> collectionPathsWithChildOperations, ISet<PropertyPath> wholeCollectionReplacementPaths, PropertiesChanged<ExpandoObject> propertiesChanged, ExpandoObject? initialState, ISet<string> nullParentPaths, IList<NullArrayParent> nullArrayParents)
    {
        var allArrayFilters = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();

        // Sink-owned system properties (e.g. __lastHandledEventSequenceNumber) are written via a
        // dedicated $max operator in BuildLastHandledEventSequenceNumber. Including them in the
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
            foreach (var parent in MissingParents(initialState, propertyDifference.PropertyPath, propertyDifference.ArrayIndexers))
            {
                var (parentProperty, parentFilters) = converter.ToMongoDBProperty(parent, propertyDifference.ArrayIndexers);
                if (parentFilters.Any())
                {
                    var filters = parentFilters.ToArray();
                    var innermost = filters[^1].Document.DeepClone().AsBsonDocument;
                    var identifier = innermost.GetElement(0).Name.Split('.')[0];
                    var marker = $".$[{identifier}].";
                    var relativePath = parentProperty[(parentProperty.LastIndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
                    innermost[$"{identifier}.{relativePath}"] = new BsonDocument("$type", "null");
                    filters[^1] = new BsonDocumentArrayFilterDefinition<BsonDocument>(innermost);
                    nullArrayParents.Add(new NullArrayParent(parentProperty, filters));
                }
                else
                {
                    nullParentPaths.Add(parentProperty);
                }
            }

            var (property, arrayFilters) = converter.ToMongoDBProperty(propertyDifference.PropertyPath, propertyDifference.ArrayIndexers);
            allArrayFilters.AddRange(arrayFilters);
            var value = converter.ToBsonValue(propertyDifference.Changed, propertyDifference.PropertyPath);

            if (updateBuilder != default)
            {
                updateBuilder = updateBuilder.Set(property, value);
            }
            else
            {
                updateBuilder = updateDefinitionBuilder.Set(property, value);
            }
        }

        arrayFiltersForDocument.AddRange(allArrayFilters);

        // Report hasChanges based on the original diff set, not the filtered one: a diff that
        // contained only filtered properties (e.g. _id or __lastHandledEventSequenceNumber) is
        // still semantically a change as far as the read-modify-write cycle is concerned, and
        // BuildLastHandledEventSequenceNumber needs to fire to advance the sequence number.
        return propertiesChanged.Differences.Any();
    }

    void BuildChildAdded(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, ChildAdded childAdded)
    {
        BsonValue bsonValue;

        var stateType = childAdded.State.GetType();
        if (stateType.IsPrimitive || stateType == typeof(string) || stateType == typeof(Guid) || stateType == typeof(DateTime) || stateType == typeof(DateTimeOffset) || stateType == typeof(DateOnly) || stateType == typeof(TimeOnly))
        {
            bsonValue = childAdded.State.ToBsonValue();
        }
        else
        {
            var schema = readModel.GetSchemaForLatestGeneration().GetSchemaForPropertyPath(childAdded.ChildrenProperty);
            bsonValue = expandoObjectConverter.ToBsonDocument((childAdded.State as ExpandoObject)!, schema);
        }

        var childrenProperty = childAdded.ChildrenProperty.GetChildrenProperty();
        var arrayIndexers = new ArrayIndexers(childAdded.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childAdded.ChildrenProperty)));
        var (property, arrayFilters) = converter.ToMongoDBProperty(childrenProperty, arrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Push(property, bsonValue)
            : updateDefinitionBuilder.Push(property, bsonValue);
    }

    void BuildNestedCleared(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, NestedCleared nestedCleared)
    {
        var (property, arrayFilters) = converter.ToMongoDBProperty(nestedCleared.NestedProperty, nestedCleared.ArrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Unset(property)
            : updateDefinitionBuilder.Unset(property);
    }

    void BuildChildRemoved(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, ChildRemoved childRemoved)
    {
        var childrenProperty = childRemoved.ChildrenProperty.GetChildrenProperty();
        var arrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childRemoved.ChildrenProperty)));
        var (property, arrayFilters) = converter.ToMongoDBProperty(childrenProperty, arrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        var stateType = childRemoved.State.GetType();
        var isPrimitive = stateType.IsPrimitive || stateType == typeof(string) || stateType == typeof(Guid) || stateType == typeof(DateTime) || stateType == typeof(DateTimeOffset) || stateType == typeof(DateOnly) || stateType == typeof(TimeOnly);
        var identifiedByProperty = childRemoved.IdentifiedByProperty.Path.ToMongoDBPropertyName();

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
            var childFilter = Builders<BsonDocument>.Filter.Eq(identifiedByProperty, childRemoved.Key.ToBsonValue());
            updateBuilder = updateBuilder is not null
                ? updateBuilder.PullFilter(property, childFilter)
                : updateDefinitionBuilder.PullFilter(property, childFilter);
            return;
        }

        var bsonValue = isPrimitive
            ? childRemoved.State.ToBsonValue()
            : expandoObjectConverter.ToBsonDocument((childRemoved.State as ExpandoObject)!, readModel.GetSchemaForLatestGeneration().GetSchemaForPropertyPath(childRemoved.ChildrenProperty));

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Pull(property, bsonValue)
            : updateDefinitionBuilder.Pull(property, bsonValue);
    }

    async Task PerformJoined(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, Joined joined, EventSequenceNumber eventSequenceNumber)
    {
        UpdateDefinition<BsonDocument>? joinUpdateBuilder = default;
        var hasJoinChanges = false;
        var collection = collections.GetCollection();

        var joinArrayFiltersForDocument = new ArrayFilters();
        var nullParentPaths = new HashSet<string>();
        var nullArrayParents = new List<NullArrayParent>();

        // A join can match many documents with different parent shapes. Check each matched
        // document for legacy nulls rather than using one joined state for the whole batch.
        await ApplyActualChanges(key, joined.Changes, null, nullParentPaths, nullArrayParents, updateDefinitionBuilder, ref joinUpdateBuilder, ref hasJoinChanges, joinArrayFiltersForDocument, eventSequenceNumber);

        if (!hasJoinChanges)
        {
            return;
        }

        var isRootLevelJoin = key.ArrayIndexers.IsEmpty;
        var target = CreateJoinFilterTarget(key, joined);

        // An absent comparand yields BsonNull, and Eq(property, null) matches every document whose column is
        // null OR missing — an UpdateMany would then stamp all of them. The SQL sink refuses the same shape
        // (Storage.Sql/Sinks/Sink.cs, ApplyJoinedChange), so refuse it here rather than write the collection.
        if (target.Value is null or BsonNull)
        {
            logger.JoinHasNoKey(readModel.Identifier, joined.OnProperty);
            return;
        }

        var joinFilter = Builders<BsonDocument>.Filter.Eq(target.Property, target.Value);
        foreach (var parent in nullParentPaths.Select(path => (Path: path, Filters: (IReadOnlyList<BsonDocumentArrayFilterDefinition<BsonDocument>>)[]))
                     .Concat(nullArrayParents.DistinctBy(_ => (_.Path, string.Join('|', _.ArrayFilters.Select(filter => filter.Document.ToJson())))).Select(_ => (_.Path, Filters: _.ArrayFilters)))
                     .OrderBy(_ => _.Path.Count(ch => ch == '.')))
        {
            var repairFilter = parent.Filters.Count == 0
                ? Builders<BsonDocument>.Filter.And(joinFilter, Builders<BsonDocument>.Filter.Type(parent.Path, BsonType.Null))
                : joinFilter;
            await collection.UpdateManyAsync(
                repairFilter,
                updateDefinitionBuilder.Unset(parent.Path),
                new UpdateOptions { ArrayFilters = parent.Filters });
        }

        BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref joinUpdateBuilder, eventSequenceNumber);
        var result = await collection.UpdateManyAsync(
            joinFilter,
            joinUpdateBuilder,
            new UpdateOptions
            {
                IsUpsert = false,
                ArrayFilters = [.. joinArrayFiltersForDocument]
            });

        // A join that matches nothing is a successful zero-row update — the write is simply lost. That is
        // legitimate when no root carries the joined value yet (the row-creation-time backfill covers it),
        // so this is diagnostic rather than a failure; without it a misdeclared join is entirely silent.
        // A CHILD join legitimately matches nothing whenever no root holds the child, which is routine, so
        // only the root-level branch reports it. The key is the join source's event source id, which is the
        // compliance subject by default — the read model and the joined-on property are the diagnostic
        // value, so the identifier itself is deliberately left out of the message.
        if (isRootLevelJoin && result.IsAcknowledged && result.MatchedCount == 0)
        {
            logger.JoinMatchedNoDocuments(readModel.Identifier, joined.OnProperty);
        }
    }

    BsonValue ToFilterValue(object? value, PropertyPath property)
    {
        try
        {
            return converter.ToBsonValue(value, property);
        }
        catch (Exception)
        {
            // The comparand is the join source's raw event source id, and the schema of the joined-on column
            // cannot always represent it — a Guid-formatted column against a key that is not a Guid is the
            // common shape, and the conversion raises a FormatException. Filtering on the unconverted value
            // matches nothing, which is exactly what this filter did before the conversion was introduced;
            // throwing instead would fail the write and freeze the partition permanently. The value is the
            // compliance subject by default, so the diagnostic names the property and not the value.
            logger.JoinKeyNotConvertible(readModel.Identifier, property);
            return value.ToBsonValue();
        }
    }

    IEnumerable<Change> NormalizeJoinedChanges(IEnumerable<Change> changes)
    {
        var normalizedChanges = changes.ToList();
        var joinedChangesToSkip = new HashSet<Joined>();

        foreach (var joined in normalizedChanges.OfType<Joined>())
        {
            if (!TryMergeJoinedChangeIntoChildAdded(normalizedChanges, joined))
            {
                continue;
            }

            joinedChangesToSkip.Add(joined);
        }

        return normalizedChanges.Where(change => change is not Joined joined || !joinedChangesToSkip.Contains(joined));
    }
}
