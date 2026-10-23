// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that resolves pending futures.
/// </summary>
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
/// <param name="futuresTracker"><see cref="ProjectionFuturesTracker"/> shared with the <see cref="StoreFutures"/> step.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for creating changesets when resolving futures.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> for retrying deferred keys.</param>
/// <param name="sink"><see cref="ISink"/> for retrying deferred keys.</param>
public class ResolveFutures(
    IProjectionFutures projectionFutures,
    ProjectionFuturesTracker futuresTracker,
    ITypeFormats typeFormats,
    IObjectComparer objectComparer,
    ILogger<ResolveFutures> logger,
    IEventSequenceStorage eventSequenceStorage,
    ISink sink) : ICanPerformProjectionPipelineStep
{
    readonly HashSet<ProjectionFutureId> _failedFutures = [];

    /// <inheritdoc/>
    /// <remarks>
    /// Deferred children use the key and state materialized by the releasing parent event, including that event.
    /// History-based retries use the same inclusive release boundary (see <see cref="ParentKeyResolutionScope"/>).
    /// Ordinary resolution remains strictly before the child; later replay-page events cannot reroute a release.
    /// </remarks>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        // If this event was deferred or key is permanently unresolvable, don't try to resolve futures
        if (context.IsDeferred || context.IsUnresolvable)
        {
            return context;
        }

        // Nothing has been stored since resolution last drained the futures store, so skip the grain call
        if (!futuresTracker.HasPending)
        {
            return context;
        }

        // Attempt to resolve any pending futures now that we've processed a new event
        var futures = await projectionFutures.GetFutures();

        _failedFutures.IntersectWith(futures.Select(future => future.Id));
        if (!futures.Any())
        {
            futuresTracker.HasPending = false;
            return context;
        }

        logger.FoundPendingFutures(futures.Count(), projection.Identifier);
        using var resolutionScope = ParentKeyResolutionScope.ForDeferredRelease(context.Event.Context.SequenceNumber);

        // Keep trying to resolve futures until we can't resolve any more
        // This handles the case where resolving one future creates the parent data needed by another future
        var resolvedAny = true;
        var latestResolvedEvent = context.Event;
        var queuedFutures = new HashSet<ProjectionFutureId>();
        while (resolvedAny)
        {
            resolvedAny = false;
            futures = await projectionFutures.GetFutures();

            foreach (var future in futures.Where(future => !queuedFutures.Contains(future.Id)))
            {
                try
                {
                    var parentIdentifierPath = future.ParentPath + future.ParentIdentifiedByProperty;
                    var type = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(parentIdentifierPath, typeFormats);
                    var parentKey = future.ParentKey.Value;
                    if (type is not null && !future.ParentPath.IsRoot)
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
                        LogFailure(new InvalidOperationException($"Could not find child projection for path '{future.ChildPath}'"), future);
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
                        context.Changeset,
                        context.Key.Value,
                        childProjection,
                        future.ParentPath,
                        future.ParentIdentifiedByProperty,
                        parentKey,
                        out var parentIndexers);

                    var retriedKey = parentExistsInCurrentState ? null : await RetryDeferredKey(childProjection, context, future);
                    if (!parentExistsInCurrentState && retriedKey is null)
                    {
                        // Parent still doesn't exist, skip this future
                        logger.ParentNotInCurrentState(future.Id);
                        continue;
                    }
                    if (retriedKey is not null)
                    {
                        parentIndexers = retriedKey.Key.ArrayIndexers.All.SkipLast(1).ToList();
                    }

                    logger.ParentExistsInCurrentState(future.Id);
                    logger.ResolvedKeyCallingOnNext(future.ParentKey, future.Id);

                    var childIdentifierPath = future.ChildPath + future.IdentifiedByProperty;
                    var childType = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(childIdentifierPath, typeFormats);

                    var childKey = retriedKey?.Key.ArrayIndexers.All.Last().Identifier ?? GetChildKey(future);

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

                    if (retriedKey is not null)
                    {
                        // A different event needs a fresh key memo, not the releasing parent's cached results.
                        futureContext = new ProjectionEventContext(
                            key,
                            future.Event,
                            futureChangeset,
                            futureContext.OperationType,
                            context.NeedsInitialState,
                            retriedKey.JoinKey ?? childKey ?? key.Value)
                        {
                            GenerationDelivery = context.GenerationDelivery,
                            IsNewInstance = context.IsNewInstance
                        };
                        futureContext.MemoizeResolvedKey(childProjection.GetKeyResolverFor(future.Event.Context.EventType), retriedKey with { Key = key });
                    }

                    ProvideToAncestors(childProjection, futureContext);
                    childProjection.OnNext(futureContext);

                    await QueueFutureSave(context, new PendingFutureSave(key, futureChangeset) { FutureId = future.Id }, queuedFutures);
                    _failedFutures.Remove(future.Id);
                    logger.ResolvedFuture(future.Id, future.ProjectionId);
                    resolvedAny = true;
                    if (future.Event.Context.SequenceNumber > latestResolvedEvent.Context.SequenceNumber)
                    {
                        latestResolvedEvent = future.Event;
                    }
                }
                catch (Exception ex)
                {
                    LogFailure(ex, future);
                }
            }
        }

        // Queued writes remain persisted too, so a failed save keeps the tracker pending.
        futuresTracker.HasPending = futures.Any();

        return context with { Event = latestResolvedEvent };
    }

    /// <summary>
    /// Provides a resolved future's event to every ancestor of the projection it belongs to, root first.
    /// </summary>
    /// <param name="childProjection">The projection the future belongs to.</param>
    /// <param name="context">The <see cref="ProjectionEventContext"/> for the resolved future.</param>
    /// <remarks>
    /// Top-down handling runs an ancestor's mappings for its descendants' events on the way to the child. Deferring the
    /// event discarded that changeset, so without this a child event arriving before its parent would leave the
    /// ancestors' every-event mappings unapplied, and the read model would differ from one built in replay order.
    /// </remarks>
    static void ProvideToAncestors(IProjection childProjection, ProjectionEventContext context)
    {
        var ancestors = new List<IProjection>();
        for (var current = childProjection.Parent; current is not null; current = current.ChildrenPropertyPath.IsRoot ? null : current.Parent)
        {
            ancestors.Insert(0, current);
        }

        foreach (var ancestor in ancestors)
        {
            ancestor.OnDescendantEvent(context with { OperationType = ancestor.GetOperationTypeFor(context.Event.Context.EventType) });
        }
    }

    static bool IsParentEvent(IProjection projection, EventTypeId eventType)
    {
        var parent = projection.Parent;
        while (parent is not null)
        {
            if (parent.EventTypes.Any(type => type.Id == eventType))
            {
                return true;
            }
            parent = parent.HasParent ? parent.Parent : null;
        }
        return false;
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
    /// <param name="changeset">The changeset for the current root event, including initialization changes.</param>
    /// <param name="rootKey">The resolved key of the root document currently being processed.</param>
    /// <param name="childProjection">The child projection whose parent chain is being resolved.</param>
    /// <param name="parentPath">The <see cref="PropertyPath"/> identifying the parent's collection.</param>
    /// <param name="parentIdentifiedByProperty">The <see cref="PropertyPath"/> of the property that identifies items in the parent collection.</param>
    /// <param name="parentKey">The key value of the parent item being located.</param>
    /// <param name="allIndexers">Receives the full root-to-parent indexer chain when the method returns <see langword="true"/>.</param>
    /// <remarks>
    /// For a first-level child the root read model is the parent, so no ancestor indexers are
    /// needed. For a child of an array element, <paramref name="allIndexers"/> receives a single
    /// entry for the parent's collection. For deeper hierarchies the method
    /// recurses through each intermediate array, locating the enclosing element at each level and
    /// recording its identifier, so that EnsurePath can navigate the full path
    /// without missing-indexer errors.
    /// </remarks>
    /// <returns><see langword="true"/> if the parent was found; otherwise <see langword="false"/>.</returns>
    static bool TryFindParentWithIndexers(
        ExpandoObject? currentState,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        object rootKey,
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
            // A first-level child can be created under the root or updated after it already
            // exists there. The creator's parent key is the root key, whereas an update whose
            // parent key comes from its event source names the child in the root collection.
            // Neither may resolve against a child-created placeholder before the root's FROM.
            if (!IsRootInitialized(currentState, changeset)) return false;
            if (KeyValueEquality.AreEqual(rootKey, parentKey)) return true;

            var directCollection = AsExpandoCollection(childProjection.ChildrenPropertyPath.GetValue(currentState, ArrayIndexers.NoIndexers));
            return directCollection?.ToList().Contains(parentIdentifiedByProperty, parentKey) == true;
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

    static bool IsRootInitialized(ExpandoObject state, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        if (changeset.HasBeenRemoved()) return false;

        var initializedNow = changeset.Changes
            .OfType<PropertiesChanged<ExpandoObject>>()
            .SelectMany(change => change.Differences)
            .LastOrDefault(difference => difference.PropertyPath.Path == WellKnownProperties.ReadModelInstanceInitialized);
        if (initializedNow is not null)
        {
            return initializedNow.Changed is true;
        }

        return ((IDictionary<string, object?>)state).TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) && initialized is true;
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
    /// using type-converting lookup so that type mismatches (e.g. <c language="csharp">EventSourceId</c> vs <c language="csharp">Guid</c>)
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

    static object? GetChildKey(ProjectionFuture future)
    {
        // Extract the child key from content: EventSourceId might be the parent when using a context key.
        // Fall back to EventSourceId only for a missing simple property (not a nested path).
        var childKey = future.IdentifiedByProperty.GetValue(future.Event.Content, ArrayIndexers.NoIndexers);
        var isSimpleProperty = !future.IdentifiedByProperty.Path.Contains('.') && !future.IdentifiedByProperty.Path.Contains('[');
        if (childKey is null && isSimpleProperty)
        {
            childKey = future.Event.Context.EventSourceId;
        }
        return childKey;
    }

    async Task<ResolvedKey?> RetryDeferredKey(IProjection childProjection, ProjectionEventContext context, ProjectionFuture future)
    {
        if (!IsParentEvent(childProjection, context.EventType.Id))
        {
            return null;
        }

        var resolver = childProjection.GetKeyResolverFor(future.Event.Context.EventType);
        if (await resolver(eventSequenceStorage, sink, future.Event) is not ResolvedKey resolved ||
            !KeyValueEquality.AreEqual(resolved.Key.Value, context.Key.Value))
        {
            return null;
        }

        var indexers = resolved.Key.ArrayIndexers.All.ToArray();
        if (indexers.Length == 0 || indexers[^1].ArrayProperty != future.ChildPath || indexers[^1].IdentifierProperty != future.IdentifiedByProperty)
        {
            return null;
        }

        var parentKey = future.ParentPath.IsRoot ? resolved.Key.Value : indexers.LastOrDefault(indexer => indexer.ArrayProperty == future.ParentPath)?.Identifier;
        if (parentKey is null || !TryFindParentWithIndexers(
            context.Changeset.CurrentState,
            context.Changeset,
            context.Key.Value,
            childProjection,
            future.ParentPath,
            future.ParentIdentifiedByProperty,
            parentKey,
            out var parentIndexers))
        {
            return null;
        }

        // A matching leaf alone is insufficient when the resolver selected a different ancestor chain.
        var expectedParents = indexers.SkipLast(1).ToArray();
        return parentIndexers.Count == expectedParents.Length && parentIndexers.Zip(expectedParents).All(pair =>
            pair.First.ArrayProperty == pair.Second.ArrayProperty &&
            pair.First.IdentifierProperty == pair.Second.IdentifierProperty &&
            KeyValueEquality.AreEqual(pair.First.Identifier, pair.Second.Identifier)) ? resolved : null;
    }

    void LogFailure(Exception exception, ProjectionFuture future)
    {
        if (_failedFutures.Add(future.Id))
        {
            logger.FailedToResolveFuture(exception, future.Id, future.ProjectionId);
        }
        else
        {
            logger.FutureStillUnresolved(exception, future.Id, future.ProjectionId);
        }
    }

    async Task QueueFutureSave(ProjectionEventContext context, PendingFutureSave pendingSave, HashSet<ProjectionFutureId> queuedFutures)
    {
        var futureId = pendingSave.FutureId!;
        if (pendingSave.Changeset.HasChanges)
        {
            context.AddPendingFutureSave(pendingSave.Key, pendingSave.Changeset, futureId);
            queuedFutures.Add(futureId);
        }
        else
        {
            await projectionFutures.ResolveFuture(futureId);
        }
    }
}
