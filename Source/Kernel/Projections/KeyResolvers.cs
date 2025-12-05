// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IKeyResolvers"/>.
/// </summary>
/// <param name="logger">The logger.</param>
public class KeyResolvers(ILogger<KeyResolvers> logger) : IKeyResolvers
{
    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides the event source id from an event.
    /// </summary>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver FromEventSourceId =>
        CreateKeyResolver(nameof(FromEventSourceId), (_, _, @event) =>
            Task.FromResult(KeyResolverResult.Resolved(new Key(EventValueProviders.EventSourceId(@event), ArrayIndexers.NoIndexers))));

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a value from the event content.
    /// </summary>
    /// <param name="eventValueProvider">The actual <see cref="ValueProvider{T}"/> for resolving key.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver FromEventValueProvider(ValueProvider<AppendedEvent> eventValueProvider) =>
        CreateKeyResolver(nameof(FromEventValueProvider), (_, _, @event) =>
        {
            var key = eventValueProvider(@event);
            return Task.FromResult(KeyResolverResult.Resolved(new Key(key, ArrayIndexers.NoIndexers)))!;
        });

    /// <inheritdoc/>
    public KeyResolver FromEventValueProviderWithFallbackToEventSourceId(ValueProvider<AppendedEvent> eventValueProvider) =>
        CreateKeyResolver(nameof(FromEventValueProviderWithFallbackToEventSourceId), (_, _, @event) =>
        {
            var key = eventValueProvider(@event);
            var willUseFallback = key is null;
            logger.FromEventValueProviderWithFallback(key, willUseFallback);

            // If the value provider returns null (property not found in event), fall back to EventSourceId
            // This supports scenarios where an event is appended to the parent's EventSourceId
            // but doesn't contain the parent key in its content
            key ??= EventValueProviders.EventSourceId(@event);

            return Task.FromResult(KeyResolverResult.Resolved(new Key(key, ArrayIndexers.NoIndexers)))!;
        });

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value which is a composite of a set of <see cref="ValueProvider{T}"/>.
    /// </summary>
    /// <param name="propertiesWithKeyValueProviders">Target property paths in key and resolvers to use for resolving.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver Composite(IDictionary<PropertyPath, ValueProvider<AppendedEvent>> propertiesWithKeyValueProviders) =>
        CreateKeyResolver(nameof(Composite), (_, _, @event) =>
        {
            var key = new ExpandoObject();
            foreach (var keyValue in propertiesWithKeyValueProviders)
            {
                var actualTarget =
                    key.EnsurePath(keyValue.Key, ArrayIndexers.NoIndexers) as IDictionary<string, object>;
                actualTarget[keyValue.Key.LastSegment.Value] = keyValue.Value(@event);
            }

            return Task.FromResult(KeyResolverResult.Resolved(new Key(key, ArrayIndexers.NoIndexers)));
        });

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value for a join relationship.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> the join is for.</param>
    /// <param name="keyResolver"><see cref="KeyResolver"/> for resolving the key from the event.</param>
    /// <param name="identifiedByProperty">The <see cref="PropertyPath"/> for the identified by property in the join relationship.</param>
    /// <returns><see cref="KeyResolver"/> that will be used to resolve.</returns>
    public KeyResolver ForJoin(IProjection projection, KeyResolver keyResolver, PropertyPath identifiedByProperty) =>
        CreateKeyResolver(nameof(ForJoin), async (eventSequenceStorage, sink, @event) =>
        {
            var keyResult = await keyResolver(eventSequenceStorage, sink, @event);

            // If the key resolution was deferred, propagate the deferred result
            if (keyResult is DeferredKey deferred)
            {
                return deferred;
            }

            var resolvedKey = (keyResult as ResolvedKey)!;
            var key = resolvedKey.Key;

            if (!projection.HasParent)
            {
                return KeyResolverResult.Resolved(key with { ArrayIndexers = ArrayIndexers.NoIndexers });
            }

            var arrayIndexers = new List<ArrayIndexer>
            {
                new(projection.ChildrenPropertyPath, identifiedByProperty, key.Value!)
            };

            return KeyResolverResult.Resolved(key with { ArrayIndexers = new ArrayIndexers(arrayIndexers) });
        });

    /// <summary>
    /// Create a <see cref="KeyResolver"/> that provides a key value hierarchically upwards in Child->Parent relationships.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to start at.</param>
    /// <param name="keyResolver"><see cref="KeyResolver"/> to use for resolving the key for the incoming event.</param>
    /// <param name="parentKeyResolver">The property that represents the parent key.</param>
    /// <param name="identifiedByProperty">The property that identifies the key on the child object.</param>
    /// <returns>A new <see cref="KeyResolver"/>.</returns>
    public KeyResolver FromParentHierarchy(IProjection projection, KeyResolver keyResolver, KeyResolver parentKeyResolver, PropertyPath identifiedByProperty) =>
        CreateKeyResolver(nameof(FromParentHierarchy), async (eventSequenceStorage, sink, @event) =>
            {
                logger.FromParentHierarchyEntry(@event.Context.EventType.Id.ToString(), @event.Context.EventSourceId.ToString(), @event.Context.SequenceNumber.Value);

                var parentKeyResult = await parentKeyResolver(eventSequenceStorage, sink, @event);

                // If parent key resolution was deferred, propagate the deferred result
                if (parentKeyResult is DeferredKey deferredParent)
                {
                    return deferredParent;
                }

                var parentKey = (parentKeyResult as ResolvedKey)!.Key;
                logger.FromParentHierarchyStart(projection.Path, projection.HasParent, parentKey.Value);

                if (!projection.HasParent)
                {
                    return KeyResolverResult.Resolved(parentKey with { ArrayIndexers = ArrayIndexers.NoIndexers });
                }

                var keyResult = await keyResolver(eventSequenceStorage, sink, @event);

                // If child key resolution was deferred, propagate the deferred result
                if (keyResult is DeferredKey deferredChild)
                {
                    return deferredChild;
                }

                var key = (keyResult as ResolvedKey)!.Key;
                logger.FromParentHierarchyChildKey(key.Value, projection.ChildrenPropertyPath, identifiedByProperty);

                var parentProjection = projection.Parent!;
                var parentEventTypeIds = parentProjection.OwnEventTypes.Select(_ => _.Id).ToArray();
                logger.FromParentHierarchyParentEventTypes(parentEventTypeIds.Length, string.Join(", ", parentEventTypeIds));

                if (parentEventTypeIds.Length == 0)
                {
                    return KeyResolverResult.Resolved(parentKey with { ArrayIndexers = new ArrayIndexers([new ArrayIndexer(projection.ChildrenPropertyPath, identifiedByProperty, key.Value)]) });
                }

                AppendedEvent parentEvent;
                var eventTypeMatchesParent = parentEventTypeIds.Any(id => id == @event.Context.EventType.Id);
                if (eventTypeMatchesParent)
                {
                    parentEvent = @event;
                }
                else
                {
                    logger.FromParentHierarchyLookupParentEvent(parentKey.Value?.ToString() ?? "null");

                    // First, try to find the parent event by EventSourceId (traditional approach)
                    var optionalEvent = await eventSequenceStorage.TryGetLastInstanceOfAny(parentKey.Value?.ToString()!, parentEventTypeIds);
                    if (!optionalEvent.TryGetValue(out var foundEvent))
                    {
                        // If not found by EventSourceId, try to find the root key by querying the Sink
                        // This supports scenarios where events are appended to a different EventSourceId
                        // but contain the parent key in their content
                        logger.FromParentHierarchyNoParentEventFound(parentKey.Value?.ToString() ?? "null");

                        // Use the PARENT projection's ChildrenPropertyPath to find where the parent lives
                        // For example, for Hub's parent Configuration, this would be [Configurations], not [Configurations].[Hubs]
                        // Then append the parent's IdentifiedByProperty to query for the specific parent
                        var parentIdentifiedByProperty = GetParentIdentifiedByProperty(parentProjection);
                        if (parentIdentifiedByProperty is null)
                        {
                            logger.FromParentHierarchyCreatingFuture(projection.Path, key.Value?.ToString() ?? "null");

                            var deferredFuture = new ProjectionFuture(
                                ProjectionFutureId.New(),
                                projection.Identifier,
                                @event,
                                parentProjection.ChildrenPropertyPath,
                                projection.ChildrenPropertyPath,
                                identifiedByProperty,
                                PropertyPath.Root,
                                parentKey.Value!,
                                DateTimeOffset.UtcNow);

                            return KeyResolverResult.Deferred(deferredFuture);
                        }

                        var childPropertyPath = parentProjection.ChildrenPropertyPath + parentIdentifiedByProperty;

                        logger.FromParentHierarchyLookupBySink(childPropertyPath.Path, parentKey.Value?.ToString() ?? "null");

                        var optionalRootKey = await sink.TryFindRootKeyByChildValue(childPropertyPath, parentKey.Value!);
                        if (optionalRootKey.TryGetValue(out var rootKey))
                        {
                            logger.FromParentHierarchyFoundRootKeyBySink(rootKey.Value?.ToString() ?? "null");

                            // Recursively resolve up the hierarchy from this point
                            // This builds the indexers in the correct order (root to leaf)
                            var hierarchyResult = await ResolveParentHierarchyFromSink(
                                parentProjection,
                                rootKey,
                                sink,
                                parentKey.Value!,
                                projection.ChildrenPropertyPath,
                                identifiedByProperty,
                                key.Value);

                            return KeyResolverResult.Resolved(hierarchyResult);
                        }

                        // Couldn't find parent in Sink - create a future for deferred resolution
                        logger.FromParentHierarchyCreatingFuture(projection.Path, key.Value?.ToString() ?? "null");

                        var future = new ProjectionFuture(
                            ProjectionFutureId.New(),
                            projection.Identifier,
                            @event,
                            parentProjection.ChildrenPropertyPath,
                            projection.ChildrenPropertyPath,
                            identifiedByProperty.Path,
                            GetParentIdentifiedByProperty(parentProjection) ?? PropertyPath.Root,
                            parentKey.Value!,
                            DateTimeOffset.UtcNow);

                        return KeyResolverResult.Deferred(future);
                    }
                    parentEvent = foundEvent;
                }

                logger.FromParentHierarchyFoundParentEvent(parentEvent.Context.EventType.Id.ToString());

                var eventType = parentProjection.EventTypes.First(eventType => eventType.Id == parentEvent.Context.EventType.Id);
                var keyResolverForEventType = parentProjection.GetKeyResolverFor(eventType);
                var resolvedParentKeyResult = await keyResolverForEventType(eventSequenceStorage, sink, parentEvent);

                // If parent resolution was deferred, propagate the deferred result
                if (resolvedParentKeyResult is DeferredKey deferredParentResolution)
                {
                    return deferredParentResolution;
                }

                var resolvedParentKey = (resolvedParentKeyResult as ResolvedKey)!.Key;
                parentKey = resolvedParentKey;

                // Build array indexers from parent down to current child
                var arrayIndexers = resolvedParentKey.ArrayIndexers.All.ToList();
                arrayIndexers.Add(new ArrayIndexer(projection.ChildrenPropertyPath, identifiedByProperty, key.Value));

                logger.FromParentHierarchyResult(parentKey.Value, arrayIndexers.Count);

                return KeyResolverResult.Resolved(parentKey with { ArrayIndexers = new ArrayIndexers(arrayIndexers) });
            });

    static PropertyPath? GetParentIdentifiedByProperty(IProjection projection)
    {
        // Get the identified by property from the projection
        // This is the property that identifies items in the children collection
        if (projection.IdentifiedByProperty.IsSet)
        {
            return projection.IdentifiedByProperty;
        }

        return null;
    }

    async Task<Key> ResolveParentHierarchyFromSink(
        IProjection projection,
        Key currentKey,
        Storage.Sinks.ISink sink,
        object intermediateKeyValue,
        PropertyPath leafChildrenProperty,
        PropertyPath leafIdentifiedByProperty,
        object leafKeyValue)
    {
        logger.ResolveParentHierarchyFromSinkStart(currentKey.Value, intermediateKeyValue, leafChildrenProperty.Path, leafIdentifiedByProperty.Path, leafKeyValue);

        // Build indexers from root down to leaf
        var arrayIndexers = new List<ArrayIndexer>();

        // Recursively walk up to collect all parent levels
        await CollectParentIndexers(projection, currentKey, sink, arrayIndexers, intermediateKeyValue);

        // Add the final leaf indexer
        arrayIndexers.Add(new ArrayIndexer(leafChildrenProperty, leafIdentifiedByProperty, leafKeyValue));

        logger.ResolveParentHierarchyFromSinkResult(arrayIndexers.Count, currentKey.Value);

        return currentKey with { ArrayIndexers = new ArrayIndexers(arrayIndexers) };
    }

    async Task CollectParentIndexers(
        IProjection projection,
        Key currentKey,
        Storage.Sinks.ISink sink,
        List<ArrayIndexer> indexers,
        object childKeyValue)
    {
        logger.CollectParentIndexersStart(projection.Path, currentKey.Value, childKeyValue);
        logger.CollectParentIndexersProjectionInfo(projection.HasParent, projection.Parent?.ChildrenPropertyPath.IsSet ?? false);

        // If this projection has a parent and is itself a nested child
        if (projection.HasParent && projection.Parent!.ChildrenPropertyPath.IsSet)
        {
            var parentProjection = projection.Parent!;
            var parentIdentifiedByProperty = GetParentIdentifiedByProperty(parentProjection);

            if (parentIdentifiedByProperty is not null)
            {
                var childPropertyPath = parentProjection.ChildrenPropertyPath + parentIdentifiedByProperty;
                logger.CollectParentIndexersLookup(childPropertyPath.Path, childKeyValue);

                var optionalRootKey = await sink.TryFindRootKeyByChildValue(childPropertyPath, childKeyValue);

                if (optionalRootKey.TryGetValue(out var rootKey))
                {
                    logger.CollectParentIndexersFoundRoot(rootKey.Value);

                    // Recursively collect parent's indexers first (to maintain root-to-leaf order)
                    await CollectParentIndexers(parentProjection, rootKey, sink, indexers, currentKey.Value!);
                }
                else
                {
                    logger.CollectParentIndexersNoRootFound();
                }
            }
        }

        // Add this level's indexer after collecting parents (maintains root-to-leaf order)
        if (projection.ChildrenPropertyPath.IsSet)
        {
            var identifiedBy = GetParentIdentifiedByProperty(projection);
            if (identifiedBy is not null)
            {
                logger.CollectParentIndexersAddingIndexer(projection.ChildrenPropertyPath.Path, identifiedBy.Path, childKeyValue);
                indexers.Add(new ArrayIndexer(projection.ChildrenPropertyPath, identifiedBy, childKeyValue));
            }
        }

        logger.CollectParentIndexersCompleted(indexers.Count);
    }

    KeyResolver CreateKeyResolver(string keyResolverName, KeyResolver keyResolver) => async (eventSequenceStorage, sink, @event) =>
    {
        try
        {
            logger.ResolvingKey(keyResolverName);
            return await keyResolver(eventSequenceStorage, sink, @event);
        }
        catch (Exception ex)
        {
            logger.ErrorResolving(ex, keyResolverName);
            throw;
        }
    };
}
