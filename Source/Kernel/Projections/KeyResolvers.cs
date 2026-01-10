// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
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
            return Task.FromResult(KeyResolverResult.Resolved(new Key(key, ArrayIndexers.NoIndexers)));
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

            return Task.FromResult(KeyResolverResult.Resolved(new Key(key, ArrayIndexers.NoIndexers)));
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

        var parentKeyResult = await ResolveParentKey(parentKeyResolver, eventSequenceStorage, sink, @event);
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

        var childKeyResult = await ResolveChildKey(keyResolver, eventSequenceStorage, sink, @event, projection.ChildrenPropertyPath, identifiedByProperty);
        if (childKeyResult is DeferredKey deferredChild)
        {
            return deferredChild;
        }

        var key = (childKeyResult as ResolvedKey)!.Key;

        var parentProjection = projection.Parent!;
        var parentEventTypeIds = parentProjection.OwnEventTypes.Select(_ => _.Id).ToArray();
        logger.FromParentHierarchyParentEventTypes(parentEventTypeIds.Length, string.Join(", ", (IEnumerable<EventTypeId>)parentEventTypeIds));

        if (parentEventTypeIds.Length == 0)
        {
            return KeyResolverResult.Resolved(parentKey with { ArrayIndexers = new ArrayIndexers([new ArrayIndexer(projection.ChildrenPropertyPath, identifiedByProperty, key.Value)]) });
        }

        var parentEventResult = await FindParentEvent(@event, parentKey, parentProjection, parentEventTypeIds, eventSequenceStorage, sink, projection, identifiedByProperty, key);
        if (parentEventResult.KeyResolverResult is not null)
        {
            return parentEventResult.KeyResolverResult;
        }

        var parentEvent = parentEventResult.ParentEvent!;
        logger.FromParentHierarchyFoundParentEvent(parentEvent.Context.EventType.Id.ToString());

        var resolvedParentKeyResult = await ResolveParentKeyFromEvent(parentProjection, parentEvent, eventSequenceStorage, sink);
        if (resolvedParentKeyResult is DeferredKey deferredParentResolution)
        {
            return deferredParentResolution;
        }

        var resolvedParentKey = (resolvedParentKeyResult as ResolvedKey)!.Key;
        parentKey = resolvedParentKey;

        var arrayIndexers = BuildArrayIndexers(resolvedParentKey, projection.ChildrenPropertyPath, identifiedByProperty, key.Value);

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

    static List<ArrayIndexer> BuildArrayIndexers(Key resolvedParentKey, PropertyPath childrenPropertyPath, PropertyPath identifiedByProperty, object? keyValue)
    {
        var arrayIndexers = resolvedParentKey.ArrayIndexers.All.ToList();
        arrayIndexers.Add(new ArrayIndexer(childrenPropertyPath, identifiedByProperty, keyValue!));
        return arrayIndexers;
    }

    async Task<KeyResolverResult> ResolveParentKey(
        KeyResolver parentKeyResolver,
        Storage.EventSequences.IEventSequenceStorage eventSequenceStorage,
        Storage.Sinks.ISink sink,
        AppendedEvent @event)
    {
        return await parentKeyResolver(eventSequenceStorage, sink, @event);
    }

    async Task<KeyResolverResult> ResolveChildKey(
        KeyResolver keyResolver,
        Storage.EventSequences.IEventSequenceStorage eventSequenceStorage,
        Storage.Sinks.ISink sink,
        AppendedEvent @event,
        PropertyPath childrenPropertyPath,
        PropertyPath identifiedByProperty)
    {
        var keyResult = await keyResolver(eventSequenceStorage, sink, @event);

        if (keyResult is not DeferredKey)
        {
            var key = (keyResult as ResolvedKey)!.Key;
            logger.FromParentHierarchyChildKey(key.Value, childrenPropertyPath, identifiedByProperty);
        }

        return keyResult;
    }

    async Task<ParentEventResult> FindParentEvent(
        AppendedEvent @event,
        Key parentKey,
        IProjection parentProjection,
        EventTypeId[] parentEventTypeIds,
        Storage.EventSequences.IEventSequenceStorage eventSequenceStorage,
        Storage.Sinks.ISink sink,
        IProjection projection,
        PropertyPath identifiedByProperty,
        Key childKey)
    {
        logger.FromParentHierarchyFindParentEventStart(@event.Context.EventType.Id.ToString(), @event.Context.EventSourceId.ToString(), parentKey.Value?.ToString() ?? "null");

        var eventTypeMatchesParent = parentEventTypeIds.Any(id => id == @event.Context.EventType.Id);
        if (eventTypeMatchesParent)
        {
            logger.FromParentHierarchyEventIsParentType(@event.Context.EventType.Id.ToString());
            return new ParentEventResult(@event, null);
        }

        logger.FromParentHierarchyLookupParentEvent(parentKey.Value?.ToString() ?? "null");

        // Try to find parent events and attempt to resolve each one
        // Continue trying if we encounter deferred resolutions
        var resolvedParentEvent = await TryResolveParentEventFromSequence(
            parentKey,
            parentEventTypeIds,
            parentProjection,
            eventSequenceStorage,
            sink);

        if (resolvedParentEvent.TryGetValue(out var parentEvent))
        {
            return new ParentEventResult(parentEvent, null);
        }

        logger.FromParentHierarchyNoParentEventFound(parentKey.Value?.ToString() ?? "null");
        logger.FromParentHierarchyFallingBackToSink();

        return await TryFindParentByFallback(parentProjection, sink, projection, identifiedByProperty, parentKey, @event, childKey);
    }

    async Task<Option<AppendedEvent>> TryResolveParentEventFromSequence(
        Key parentKey,
        EventTypeId[] parentEventTypeIds,
        IProjection parentProjection,
        Storage.EventSequences.IEventSequenceStorage eventSequenceStorage,
        Storage.Sinks.ISink sink)
    {
        // First, try the most recent event (traditional approach)
        logger.FromParentHierarchyTryGetLastInstance(parentKey.Value?.ToString() ?? "null", string.Join(", ", parentEventTypeIds.Select(id => id.ToString())));
        var optionalEvent = await eventSequenceStorage.TryGetLastInstanceOfAny(parentKey.Value?.ToString()!, parentEventTypeIds);
        if (!optionalEvent.TryGetValue(out var lastEvent))
        {
            logger.FromParentHierarchyNoEventFoundByTryGetLast();
            return Option<AppendedEvent>.None();
        }

        logger.FromParentHierarchyFoundEventByTryGetLast(lastEvent.Context.EventType.Id.ToString(), lastEvent.Context.SequenceNumber.Value);

        // Check if this event resolves successfully
        var eventType = parentProjection.EventTypes.FirstOrDefault(et => et.Id == lastEvent.Context.EventType.Id);
        if (eventType is not null)
        {
            logger.FromParentHierarchyResolvingLastEvent(lastEvent.Context.EventType.Id.ToString(), lastEvent.Context.SequenceNumber.Value);
            var keyResolverForEventType = parentProjection.GetKeyResolverFor(eventType);
            var resolvedKeyResult = await keyResolverForEventType(eventSequenceStorage, sink, lastEvent);

            // If this event resolves successfully (not deferred), use it
            if (resolvedKeyResult is ResolvedKey resolvedKey)
            {
                logger.FromParentHierarchyLastEventResolved(resolvedKey.Key.Value?.ToString() ?? "null");
                return new Option<AppendedEvent>(lastEvent);
            }

            logger.FromParentHierarchySkippingDeferredParentEvent(lastEvent.Context.SequenceNumber.Value);
        }
        else
        {
            logger.FromParentHierarchyLastEventTypeNotFound(lastEvent.Context.EventType.Id.ToString());
        }

        // The most recent event was deferred or couldn't be resolved
        // Try to find older events and check if any of them can be resolved
        var eventSourceId = parentKey.Value?.ToString()!;
        var headSequenceNumber = await eventSequenceStorage.GetHeadSequenceNumber(
            parentEventTypeIds.Select(id => new EventType(id, EventTypeGeneration.First)),
            eventSourceId);

        if (headSequenceNumber == EventSequenceNumber.Unavailable)
        {
            return Option<AppendedEvent>.None();
        }

        // Get all events up to (but not including) the last event we already tried
        var eventTypes = parentEventTypeIds.Select(id => new EventType(id, EventTypeGeneration.First));
        var cursor = await eventSequenceStorage.GetRange(headSequenceNumber, lastEvent.Context.SequenceNumber - 1, eventSourceId, eventTypes);

        var candidateEvents = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            candidateEvents.AddRange(cursor.Current);
        }

        // Try events from most recent to oldest
        for (var i = candidateEvents.Count - 1; i >= 0; i--)
        {
            var candidateEvent = candidateEvents[i];
            eventType = parentProjection.EventTypes.FirstOrDefault(et => et.Id == candidateEvent.Context.EventType.Id);
            if (eventType is null)
            {
                continue;
            }

            var keyResolverForEventType = parentProjection.GetKeyResolverFor(eventType);
            var resolvedKeyResult = await keyResolverForEventType(eventSequenceStorage, sink, candidateEvent);

            // If this event resolves successfully (not deferred), use it
            if (resolvedKeyResult is ResolvedKey)
            {
                logger.FromParentHierarchyFoundParentEventAfterTrying(candidateEvents.Count - i, candidateEvents.Count + 1);
                return new Option<AppendedEvent>(candidateEvent);
            }

            // If deferred, continue trying other events
            logger.FromParentHierarchySkippingDeferredParentEvent(candidateEvent.Context.SequenceNumber.Value);
        }

        // All events were deferred or couldn't be resolved
        return Option<AppendedEvent>.None();
    }

    async Task<ParentEventResult> TryFindParentByFallback(
        IProjection parentProjection,
        Storage.Sinks.ISink sink,
        IProjection projection,
        PropertyPath identifiedByProperty,
        Key parentKey,
        AppendedEvent @event,
        Key childKey)
    {
        logger.FromParentHierarchyTryFindByFallbackStart(parentProjection.Path, parentKey.Value?.ToString() ?? "null");

        var parentIdentifiedByProperty = GetParentIdentifiedByProperty(parentProjection);
        if (parentIdentifiedByProperty is null)
        {
            logger.FromParentHierarchyNoParentIdentifiedBy(parentProjection.Path);
            var deferredFuture = CreateDeferredFutureObject(projection, @event, parentProjection.ChildrenPropertyPath, identifiedByProperty, PropertyPath.Root, parentKey);
            return new ParentEventResult(null, KeyResolverResult.Deferred(deferredFuture));
        }

        // For root-level parents (ChildrenPropertyPath not set), use the child's path
        // For nested parents, use the parent's path to query at the correct level
        var childPropertyPath = parentProjection.ChildrenPropertyPath.IsSet
            ? parentProjection.ChildrenPropertyPath + parentIdentifiedByProperty
            : projection.ChildrenPropertyPath + parentIdentifiedByProperty;
        logger.FromParentHierarchyLookupBySink(childPropertyPath.Path, parentKey.Value?.ToString() ?? "null");

        logger.FromParentHierarchySinkQuery(childPropertyPath.Path, parentKey.Value?.ToString() ?? "null");
        var optionalRootKey = await sink.TryFindRootKeyByChildValue(childPropertyPath, parentKey.Value!);
        if (optionalRootKey.TryGetValue(out var rootKey))
        {
            logger.FromParentHierarchyFoundRootKeyBySink(rootKey.Value?.ToString() ?? "null");

            var hierarchyResult = await ResolveParentHierarchyFromSink(
                parentProjection,
                rootKey,
                sink,
                parentKey.Value!,
                projection.ChildrenPropertyPath,
                identifiedByProperty,
                childKey.Value);

            return new ParentEventResult(null, KeyResolverResult.Resolved(hierarchyResult));
        }

        logger.FromParentHierarchySinkDidNotFindRoot(childPropertyPath.Path, parentKey.Value?.ToString() ?? "null");

        // Determine which identifier to use:
        // - For root parents: use the child's identifier (how the child is identified in the root's collection)
        // - For nested parents: use the parent's identifier (how the parent is identified in its parent's collection)
        var parentIdentifierForFuture = parentProjection.HasParent
            ? parentProjection.IdentifiedByProperty
            : projection.IdentifiedByProperty;

        var future = CreateDeferredFutureObject(
            projection,
            @event,
            parentProjection.ChildrenPropertyPath,
            identifiedByProperty,
            parentIdentifierForFuture,
            parentKey);

        return new ParentEventResult(null, KeyResolverResult.Deferred(future));
    }

    ProjectionFuture CreateDeferredFutureObject(
        IProjection projection,
        AppendedEvent @event,
        PropertyPath parentChildrenPropertyPath,
        PropertyPath identifiedByProperty,
        PropertyPath parentIdentifiedByProperty,
        Key parentKey)
    {
        logger.FromParentHierarchyCreatingFuture(projection.Path, parentKey.Value?.ToString() ?? "null");

        var futureId = ProjectionFutureId.New();

        return new ProjectionFuture(
            futureId,
            projection.Identifier,
            @event,
            parentChildrenPropertyPath,
            projection.ChildrenPropertyPath,
            identifiedByProperty,
            parentIdentifiedByProperty,
            parentKey,
            DateTimeOffset.UtcNow);
    }

    async Task<KeyResolverResult> ResolveParentKeyFromEvent(
        IProjection parentProjection,
        AppendedEvent parentEvent,
        Storage.EventSequences.IEventSequenceStorage eventSequenceStorage,
        Storage.Sinks.ISink sink)
    {
        var eventType = parentProjection.EventTypes.First(eventType => eventType.Id == parentEvent.Context.EventType.Id);
        var keyResolverForEventType = parentProjection.GetKeyResolverFor(eventType);
        return await keyResolverForEventType(eventSequenceStorage, sink, parentEvent);
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
            var identifiedBy = projection.IdentifiedByProperty;
            if (identifiedBy.IsSet)
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

    sealed record ParentEventResult(AppendedEvent? ParentEvent, KeyResolverResult? KeyResolverResult);
}
