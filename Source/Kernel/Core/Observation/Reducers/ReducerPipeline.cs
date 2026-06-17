// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerPipeline"/> class.
/// </remarks>
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
/// <param name="sink"><see cref="ISink"/> to use in pipeline.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for encrypting and decrypting PII fields.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> this pipeline belongs to.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> this pipeline belongs to.</param>
public class ReducerPipeline(
    ReadModelDefinition readModel,
    ISink sink,
    IObjectComparer objectComparer,
    IReadModelsCompliance readModelsCompliance,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace) : IReducerPipeline
{
    /// <inheritdoc/>
    public ReadModelDefinition ReadModel { get; } = readModel;

    /// <inheritdoc/>
    public ISink Sink { get; } = sink;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Sink.BeginReplay(context);

    /// <inheritdoc/>
    public Task EndReplay(ReplayContext context) => Sink.EndReplay(context);

    /// <inheritdoc/>
    public Task BeginBulk() => Sink.BeginBulk();

    /// <inheritdoc/>
    public Task EndBulk() => Sink.EndBulk();

    /// <inheritdoc/>
    public async Task Handle(ReducerContext context, ReducerDelegate reducer)
    {
        var schema = ReadModel.GetSchemaForLatestGeneration();
        var initial = await Sink.FindOrDefault(context.Key);

        if (initial is not null)
        {
            initial = await readModelsCompliance.Release(
                eventStore,
                eventStoreNamespace,
                schema,
                initial);
        }

        var result = await reducer(context.Events, initial);

        if (result.ObserverResult.State != ObserverSubscriberState.Ok) return;

        var identifier = context.Events.First().Context.Subject.Value;

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, context.Events.First(), initial ?? new ExpandoObject());
        if (result.ReadModelState is null)
        {
            if (initial is not null)
            {
                changeset.Add(new Removed(initial));
            }
        }
        else
        {
            var encryptedState = await readModelsCompliance.Apply(
                eventStore,
                eventStoreNamespace,
                schema,
                identifier,
                result.ReadModelState);

            if (!objectComparer.Compare(initial, encryptedState, out var differences))
            {
                // The comparer has no child identity for reducer-owned collections. A nested,
                // unindexed array path cannot be applied safely by sinks, so replace that collection.
                changeset.Add(new PropertiesChanged<ExpandoObject>(
                    null!,
                    CollapseUnindexedCollectionDifferences(initial, encryptedState, differences)));
            }
        }

        if (changeset.HasChanges)
        {
            var failedPartitions = await Sink.ApplyChanges(context.Key, changeset, context.Events.Last().Context.SequenceNumber);

            if (failedPartitions.Any())
            {
                var firstFailure = failedPartitions.First();
                throw new InvalidOperationException($"Bulk operation failed for partition {firstFailure.EventSourceId} at sequence number {firstFailure.EventSequenceNumber}");
            }
        }
    }

    static List<PropertyDifference> CollapseUnindexedCollectionDifferences(
        ExpandoObject? initial,
        ExpandoObject changed,
        IEnumerable<PropertyDifference> differences)
    {
        var result = new List<PropertyDifference>();
        if (differences is null)
        {
            return result;
        }

        var collapsed = new HashSet<(PropertyPath PropertyPath, ArrayIndexers ArrayIndexers)>();

        foreach (var difference in differences)
        {
            if (!TryGetUnindexedCollectionPath(difference, out var collectionPath))
            {
                result.Add(difference);
                continue;
            }

            var collapsedKey = (collectionPath, difference.ArrayIndexers);
            if (!collapsed.Add(collapsedKey))
            {
                continue;
            }

            result.Add(new PropertyDifference(
                collectionPath,
                GetValueAtPath(initial, collectionPath, difference.ArrayIndexers),
                GetValueAtPath(changed, collectionPath, difference.ArrayIndexers),
                difference.ArrayIndexers));
        }

        return result;
    }

    static bool TryGetUnindexedCollectionPath(PropertyDifference difference, out PropertyPath collectionPath)
    {
        collectionPath = PropertyPath.NotSet;

        var currentPath = PropertyPath.Root;
        var segments = difference.PropertyPath.Segments.ToArray();

        for (var segmentIndex = 0; segmentIndex < segments.Length - 1; segmentIndex++)
        {
            var segment = segments[segmentIndex];
            currentPath += segment;

            if (segment is ArrayProperty && !difference.ArrayIndexers.HasFor(currentPath))
            {
                collectionPath = currentPath;
                return true;
            }
        }

        return false;
    }

    static object? GetValueAtPath(ExpandoObject? instance, PropertyPath propertyPath, ArrayIndexers arrayIndexers)
    {
        object? current = instance;
        var currentPath = PropertyPath.Root;

        foreach (var segment in propertyPath.Segments)
        {
            if (current is not ExpandoObject expandoObject)
            {
                return null;
            }

            if (!(expandoObject as IDictionary<string, object?>).TryGetValue(segment.Value, out current))
            {
                return null;
            }

            currentPath += segment;

            if (segment is ArrayProperty && arrayIndexers.HasFor(currentPath))
            {
                current = GetElementForIndexer(current, arrayIndexers.GetFor(currentPath));
            }
        }

        return current;
    }

    static object? GetElementForIndexer(object? collection, ArrayIndexer indexer)
    {
        if (collection is not IEnumerable enumerable)
        {
            return null;
        }

        var elements = enumerable.Cast<object>().ToArray();
        if (!indexer.IdentifierProperty.IsSet && indexer.Identifier is int index)
        {
            return elements.Length > index ? elements[index] : null;
        }

        return elements
            .OfType<ExpandoObject>()
            .Cast<IDictionary<string, object?>>()
            .SingleOrDefault(_ =>
                _.TryGetValue(indexer.IdentifierProperty.Path, out var identifier) &&
                Equals(identifier, indexer.Identifier));
    }
}
