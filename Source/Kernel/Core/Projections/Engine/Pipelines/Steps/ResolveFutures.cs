// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that resolves pending futures.
/// </summary>
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for creating changesets when resolving futures.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ResolveFutures(
    IProjectionFutures projectionFutures,
    ITypeFormats typeFormats,
    IObjectComparer objectComparer,
    ILogger<ResolveFutures> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        // If this event was deferred or key is permanently unresolvable, don't try to resolve futures
        if (context.IsDeferred || context.IsUnresolvable)
        {
            return context;
        }

        // Attempt to resolve any pending futures now that we've processed a new event
        var futures = await projectionFutures.GetFutures();

        if (!futures.Any())
        {
            return context;
        }

        logger.FoundPendingFutures(futures.Count(), projection.Identifier);

        // Keep trying to resolve futures until we can't resolve any more
        // This handles the case where resolving one future creates the parent data needed by another future
        var resolvedAny = true;
        var latestResolvedEvent = context.Event;
        while (resolvedAny)
        {
            resolvedAny = false;
            futures = await projectionFutures.GetFutures();

            foreach (var future in futures)
            {
                try
                {
                    var parentIdentifierPath = future.ParentPath + future.ParentIdentifiedByProperty;
                    var type = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(parentIdentifierPath, typeFormats);
                    var parentKey = future.ParentKey.Value;
                    if (type is not null)
                    {
                        try
                        {
                            parentKey = TypeConversion.Convert(type, parentKey);
                        }
                        catch (InvalidCastException)
                        {
                            // Keep original value — Contains() normalizes types during comparison
                        }
                    }

                    // Find the child projection that this future belongs to by navigating the property path
                    var childProjection = FindChildProjectionByPath(projection, future.ChildPath);
                    if (childProjection is null)
                    {
                        logger.FailedToResolveFuture(new InvalidOperationException($"Could not find child projection for path '{future.ChildPath}'"), future.Id, future.ProjectionId);
                        continue;
                    }

                    logger.FoundChildProjection(childProjection.Path, future.Id);
                    logger.CheckingParentInCurrentState(future.Id);

                    // Check if parent exists in the current changeset's state, collecting the full indexer
                    // chain needed to navigate from root to the parent. For parents nested beyond one level,
                    // this searches through intermediate array collections to find the matching parent and
                    // records the identifier of each intermediate element.
                    var parentExistsInCurrentState = TryFindParentWithIndexers(
                        context.Changeset.CurrentState,
                        childProjection,
                        future.ParentPath,
                        future.ParentIdentifiedByProperty,
                        parentKey,
                        out var parentIndexers);

                    if (!parentExistsInCurrentState)
                    {
                        // Parent still doesn't exist, skip this future
                        logger.ParentNotInCurrentState(future.Id);
                        continue;
                    }

                    logger.ParentExistsInCurrentState(future.Id);
                    logger.ResolvedKeyCallingOnNext(future.ParentKey, future.Id);

                    var childIdentifierPath = future.ChildPath + future.IdentifiedByProperty;
                    var childType = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(childIdentifierPath, typeFormats);

                    // Extract the child key from the event content using the IdentifiedByProperty path
                    // This ensures we get the correct identifier (e.g., HubId) rather than using EventSourceId
                    // which might be the parent's identifier when UsingParentKeyFromContext is used.
                    // If the property doesn't exist in the event content (returns null), and the IdentifiedByProperty
                    // is a simple property name (not a nested path), fall back to using EventSourceId.
                    // This handles cases like WeightsSetForSimulationConfiguration where the ConfigurationId
                    // is the EventSourceId and not in the event content.
                    var childKey = future.IdentifiedByProperty.GetValue(future.Event.Content, ArrayIndexers.NoIndexers);
                    if (childKey is null && !future.IdentifiedByProperty.Path.Contains('.') && !future.IdentifiedByProperty.Path.Contains('['))
                    {
                        // Property doesn't exist in event content and it's a simple property name,
                        // so use EventSourceId (which is how the event was originally sourced)
                        childKey = future.Event.Context.EventSourceId;
                    }

                    if (childType is not null && childKey is not null)
                    {
                        try
                        {
                            childKey = TypeConversion.Convert(childType, childKey);
                        }
                        catch
                        {
                            // Keep original value — Contains() normalizes types during comparison
                        }
                    }

                    // Build the key using the full parent indexer chain plus the child indexer.
                    // parentIndexers contains one entry per ancestor level (root → parent), giving
                    // EnsurePath everything it needs to navigate deeply nested arrays.
                    parentIndexers.Add(new ArrayIndexer(future.ChildPath, future.IdentifiedByProperty, childKey!));
                    var key = new Key(context.Key.Value, new ArrayIndexers(parentIndexers));

                    // Use a separate changeset so the future's ChildAdded is saved in its own MongoDB
                    // operation after the main changeset save, avoiding MongoDB's restriction on
                    // pushing to a child of an array element being pushed in the same update.
                    var futureChangeset = new Changeset<AppendedEvent, ExpandoObject>(
                        objectComparer,
                        future.Event,
                        context.Changeset.CurrentState);

                    var futureContext = context with
                    {
                        Event = future.Event,
                        Key = key,
                        Changeset = futureChangeset,
                        OperationType = childProjection.GetOperationTypeFor(future.Event.Context.EventType),
                        JoinKey = childKey ?? key.Value
                    };

                    childProjection.OnNext(futureContext);

                    // Successfully resolved the future
                    await projectionFutures.ResolveFuture(future.Id);
                    logger.ResolvedFuture(future.Id, future.ProjectionId);
                    resolvedAny = true;
                    if (future.Event.Context.SequenceNumber > latestResolvedEvent.Context.SequenceNumber)
                    {
                        latestResolvedEvent = future.Event;
                    }

                    if (futureChangeset.HasChanges)
                    {
                        context.AddPendingFutureSave(key, futureChangeset);
                    }
                }
                catch (Exception ex)
                {
                    logger.FailedToResolveFuture(ex, future.Id, future.ProjectionId);
                }
            }
        }

        return context with { Event = latestResolvedEvent };
    }

    static IProjection? FindChildProjectionByPath(IProjection projection, PropertyPath childPath)
    {
        // Check if this projection's ChildrenPropertyPath matches the target path
        if (projection.ChildrenPropertyPath.Path == childPath.Path)
        {
            return projection;
        }

        // Recursively search through child projections
        foreach (var child in projection.ChildProjections)
        {
            var found = FindChildProjectionByPath(child, childPath);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether the parent item exists in the current state and, if so, collects the full
    /// chain of <see cref="ArrayIndexer"/> entries needed to navigate from root to that parent.
    /// </summary>
    /// <param name="currentState">The root <see cref="ExpandoObject"/> representing the current read model state.</param>
    /// <param name="childProjection">The child projection whose parent chain is being resolved.</param>
    /// <param name="parentPath">The <see cref="PropertyPath"/> identifying the parent's collection.</param>
    /// <param name="parentIdentifiedByProperty">The <see cref="PropertyPath"/> of the property that identifies items in the parent collection.</param>
    /// <param name="parentKey">The key value of the parent item being located.</param>
    /// <param name="allIndexers">Receives the full root-to-parent indexer chain when the method returns <see langword="true"/>.</param>
    /// <remarks>
    /// For a direct child (one array level between root and parent), <paramref name="allIndexers"/>
    /// receives a single entry for the parent's collection. For deeper hierarchies the method
    /// recurses through each intermediate array, locating the enclosing element at each level and
    /// recording its identifier, so that EnsurePath can navigate the full path
    /// without missing-indexer errors.
    /// </remarks>
    /// <returns><see langword="true"/> if the parent was found; otherwise <see langword="false"/>.</returns>
    static bool TryFindParentWithIndexers(
        ExpandoObject? currentState,
        IProjection childProjection,
        PropertyPath parentPath,
        PropertyPath parentIdentifiedByProperty,
        object parentKey,
        out List<ArrayIndexer> allIndexers)
    {
        allIndexers = [];
        if (currentState is null) return false;

        var chain = BuildParentChain(childProjection);

        if (chain.Count == 0)
        {
            // The child projection's parent is root, so the resolved item is a direct child of the root.
            // Use the child projection's ChildrenPropertyPath (e.g. [Configurations]) to find the
            // collection. ParentPath is PropertyPath.Root for first-level children and cannot be used
            // directly. The caller adds the child's own ArrayIndexer; no ancestor indexers are needed.
            var directCollectionValue = childProjection.ChildrenPropertyPath.GetValue(currentState, ArrayIndexers.NoIndexers);
            var directCollection = AsExpandoCollection(directCollectionValue);
            if (directCollection is null) return false;
            var directList = directCollection.ToList();

            return directList.Contains(parentIdentifiedByProperty, parentKey);
        }

        if (chain.Count == 1)
        {
            var collectionValue = chain[0].ChildrenPropertyPath.GetValue(currentState, ArrayIndexers.NoIndexers);
            var collection = AsExpandoCollection(collectionValue);
            if (collection is null) return false;
            var collectionList = collection.ToList();
            if (!collectionList.Contains(parentIdentifiedByProperty, parentKey)) return false;

            var actualKey = GetActualStoredKey(collectionList, parentIdentifiedByProperty, parentKey);
            allIndexers.Add(new ArrayIndexer(parentPath, parentIdentifiedByProperty, actualKey));
            return true;
        }

        return SearchLevel(currentState, chain, 0, [], parentIdentifiedByProperty, parentKey, allIndexers);
    }

    /// <summary>
    /// Builds the projection chain from the root-level child projection down to the direct parent of
    /// <paramref name="childProjection"/>, in root-to-leaf order (index 0 = closest to root).
    /// </summary>
    /// <param name="childProjection">The projection whose ancestor chain is being built.</param>
    /// <returns>Ordered list of ancestor projections, from root-most to the direct parent.</returns>
    static List<IProjection> BuildParentChain(IProjection childProjection)
    {
        var chain = new List<IProjection>();
        var current = childProjection.Parent;
        while (current is not null)
        {
            var path = current.ChildrenPropertyPath;
            if (!path.IsSet || path.IsRoot) break;

            chain.Insert(0, current);
            current = current.Parent;
        }

        return chain;
    }

    /// <summary>
    /// Recursively navigates the current state through one level of the projection chain, searching
    /// every element of the current array for a descendant that ultimately contains the target parent
    /// key. When found, the full indexer chain (one entry per level, root-to-leaf) is written to
    /// <paramref name="resultIndexers"/>.
    /// </summary>
    /// <param name="rootState">The root <see cref="ExpandoObject"/> representing the current read model state.</param>
    /// <param name="chain">The ordered projection chain from root-most ancestor to the direct parent.</param>
    /// <param name="level">The current depth within <paramref name="chain"/>.</param>
    /// <param name="currentIndexers">Array indexers accumulated so far for levels above this one.</param>
    /// <param name="parentIdentifiedByProperty">The property that identifies items in the leaf-level collection.</param>
    /// <param name="parentKey">The key value to locate in the leaf-level collection.</param>
    /// <param name="resultIndexers">Receives the complete root-to-parent indexer chain on success.</param>
    /// <returns><see langword="true"/> if the parent was found at or below this level; otherwise <see langword="false"/>.</returns>
    static bool SearchLevel(
        ExpandoObject rootState,
        List<IProjection> chain,
        int level,
        List<ArrayIndexer> currentIndexers,
        PropertyPath parentIdentifiedByProperty,
        object parentKey,
        List<ArrayIndexer> resultIndexers)
    {
        var proj = chain[level];
        var childrenPath = proj.ChildrenPropertyPath;
        var isLeaf = level == chain.Count - 1;

        // Navigate from the root using the full children path with accumulated indexers so
        // that multi-segment paths (e.g. "[Configurations].[Hubs]") are resolved correctly.
        var collectionValue = childrenPath.GetValue(rootState, new ArrayIndexers(currentIndexers));
        var collection = AsExpandoCollection(collectionValue);
        if (collection is null) return false;

        if (isLeaf)
        {
            var leafList = collection.ToList();
            if (!leafList.Contains(parentIdentifiedByProperty, parentKey)) return false;

            resultIndexers.AddRange(currentIndexers);
            var actualKey = GetActualStoredKey(leafList, parentIdentifiedByProperty, parentKey);
            resultIndexers.Add(new ArrayIndexer(childrenPath, parentIdentifiedByProperty, actualKey));
            return true;
        }

        var identifiedBy = proj.IdentifiedByProperty;
        foreach (var item in collection)
        {
            if (item is not IDictionary<string, object> itemDict) continue;
            if (!itemDict.TryGetValue(identifiedBy.Path, out var itemId) || itemId is null) continue;

            var nextIndexers = new List<ArrayIndexer>(currentIndexers)
            {
                new(childrenPath, identifiedBy, itemId)
            };

            if (SearchLevel(rootState, chain, level + 1, nextIndexers, parentIdentifiedByProperty, parentKey, resultIndexers))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the value actually stored in the collection for the given identity property and key,
    /// using type-converting lookup so that type mismatches (e.g. <c>EventSourceId</c> vs <c>Guid</c>)
    /// are resolved. Falls back to <paramref name="key"/> when the item cannot be found.
    /// </summary>
    /// <param name="collection">The collection to search.</param>
    /// <param name="identifiedByProperty">The property that holds the identifier on each item.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The stored identifier value, or <paramref name="key"/> if not found.</returns>
    static object GetActualStoredKey(IEnumerable<ExpandoObject> collection, PropertyPath identifiedByProperty, object key)
    {
        var item = collection.GetItem(identifiedByProperty, key);
        if (item is IDictionary<string, object> dict
            && dict.TryGetValue(identifiedByProperty.Path, out var storedValue)
            && storedValue is not null)
        {
            return storedValue;
        }

        return key;
    }

    static IEnumerable<ExpandoObject>? AsExpandoCollection(object? value) =>
        value switch
        {
            IEnumerable<ExpandoObject> expando => expando,
            IEnumerable enumerable => enumerable.OfType<ExpandoObject>(),
            _ => null
        };
}
