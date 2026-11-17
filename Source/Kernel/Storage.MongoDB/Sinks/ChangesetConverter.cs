// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
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
/// <param name="writes"><see cref="ISinkWrites"/> executing the joined-document writes, fenced or not.</param>
public partial class ChangesetConverter(
    ReadModelDefinition readModel,
    IMongoDBConverter converter,
    ISinkCollections collections,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ChangesetConverter> logger,
    ISinkWrites writes) : IChangesetConverter, IChangesetConverterWithDirectWrites
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangesetConverter"/> class writing without a fence.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
    /// <param name="converter"><see cref="IMongoDBConverter"/> to use.</param>
    /// <param name="collections"><see cref="ISinkCollections"/> to use.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/> for logging.</param>
    public ChangesetConverter(
        ReadModelDefinition readModel,
        IMongoDBConverter converter,
        ISinkCollections collections,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<ChangesetConverter> logger)
        : this(readModel, converter, collections, expandoObjectConverter, logger, UnfencedSinkWrites.Instance)
    {
    }

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
    public Task<UpdateDefinitionAndArrayFilters> ToUpdateDefinition(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber) => ToUpdateDefinition(key, changeset, eventSequenceNumber, new SinkMultiDocumentWrites(collections, writes));

    /// <inheritdoc/>
    Task<UpdateDefinitionAndArrayFilters> IChangesetConverterWithDirectWrites.ToUpdateDefinition(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkMultiDocumentWrites directWrites) =>
        ToUpdateDefinition(key, changeset, eventSequenceNumber, directWrites);

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

    async Task<UpdateDefinitionAndArrayFilters> ToUpdateDefinition(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkMultiDocumentWrites directWrites)
    {
        var updates = new UpdateAccumulator();
        var normalizedChanges = JoinedChangeNormalizer.Normalize(changeset.Changes);

        await ApplyActualChanges(key, normalizedChanges, changeset.InitialState, updates, eventSequenceNumber, directWrites);

        // EventSequenceNumber sentinels (Unavailable, Max) are not positions in the sequence. Letting one through
        // would pin the $max watermark at the top of the range and permanently block every later guarded write to
        // the document.
        if (updates.HasChanges && eventSequenceNumber.IsActualValue)
        {
            AddLastHandledEventSequenceNumber(updates, eventSequenceNumber);
        }

        var distinctArrayFilters = updates.ArrayFilters.DistinctBy(_ => _.Document).ToArray();

        return new(updates.Definition!, distinctArrayFilters, updates.HasChanges)
        {
            NullParentPaths = updates.OrderedNullParentPaths(),
            NullArrayParents = updates.DistinctNullArrayParents(),
            RequiredArrayPaths = [.. updates.ArrayFilters.ArrayPaths]
        };
    }

    Task ApplyActualChanges(
        Key key,
        IEnumerable<Change> changes,
        ExpandoObject? initialState,
        UpdateAccumulator updates,
        EventSequenceNumber eventSequenceNumber,
        SinkMultiDocumentWrites directWrites)
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
                    updates.HasChanges |= ApplyPropertiesChanged(updates, collectionPathsWithChildOperations, wholeCollectionReplacementPaths, propertiesChanged, initialState);
                    break;

                case ChildAdded childAdded:
                    ApplyChildAdded(updates, childAdded);
                    updates.HasChanges = true;
                    break;

                case ChildRemoved childRemoved:
                    ApplyChildRemoved(key, updates, childRemoved);
                    updates.HasChanges = true;
                    break;

                case NestedCleared nestedCleared:
                    ApplyNestedCleared(updates, nestedCleared);
                    updates.HasChanges = true;
                    break;

                case Joined joined:
                    joinTasks.Add(PerformJoined(key, joined, eventSequenceNumber, directWrites));
                    break;

                case ResolvedJoin resolvedJoined:
                    joinTasks.Add(ApplyActualChanges(key, resolvedJoined.Changes, initialState, updates, eventSequenceNumber, directWrites));
                    break;
            }
        }

        return Task.WhenAll(joinTasks);
    }

    void AddLastHandledEventSequenceNumber(UpdateAccumulator updates, EventSequenceNumber eventSequenceNumber)
    {
        var value = converter.ToBsonValue(eventSequenceNumber);

        // Use $max so this value can only ever move forward in MongoDB. When events for the
        // same read-model key are processed out of order — which happens when catch-up
        // dispatches per-partition steps for a constant-key or join projection — a plain $set
        // would let an earlier sequence number overwrite a later one, leaving the read model
        // pointing at a sequence number it has already moved past.
        updates.Max(WellKnownProperties.LastHandledEventSequenceNumber, value);
    }

    async Task PerformJoined(Key key, Joined joined, EventSequenceNumber eventSequenceNumber, SinkMultiDocumentWrites directWrites)
    {
        var joinUpdates = new UpdateAccumulator();

        // A join can match many documents with different parent shapes. Check each matched
        // document for legacy nulls rather than using one joined state for the whole batch.
        await ApplyActualChanges(key, joined.Changes, null, joinUpdates, eventSequenceNumber, directWrites);

        if (!joinUpdates.HasChanges)
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
        await RepairNullParents(joinFilter, joinUpdates, directWrites);

        AddLastHandledEventSequenceNumber(joinUpdates, eventSequenceNumber);
        var result = await directWrites.UpdateManyAsync(
            joinFilter,
            joinUpdates.Definition!,
            new UpdateOptions
            {
                IsUpsert = false,
                ArrayFilters = [.. joinUpdates.ArrayFilters]
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

    async Task RepairNullParents(FilterDefinition<BsonDocument> joinFilter, UpdateAccumulator joinUpdates, SinkMultiDocumentWrites directWrites)
    {
        foreach (var parent in joinUpdates.NullParentRepairs())
        {
            var repairFilter = Builders<BsonDocument>.Filter.And(joinFilter, parent.ToNullFilter());
            await directWrites.UpdateManyAsync(
                repairFilter,
                Builders<BsonDocument>.Update.Unset(parent.Path),
                new UpdateOptions { ArrayFilters = parent.Filters });
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
}
