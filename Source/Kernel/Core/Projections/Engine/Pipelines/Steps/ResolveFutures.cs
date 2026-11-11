// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that resolves pending futures.
/// </summary>
public class ResolveFutures : ICanPerformProjectionPipelineStep
{
    readonly HashSet<ProjectionFutureId> _failedFutures = [];
    readonly IProjectionFuturesStore _projectionFutures;
    readonly ProjectionFuturesTracker _futuresTracker;
    readonly ITypeFormats _typeFormats;
    readonly IObjectComparer _objectComparer;
    readonly ICanProtectFutureChangesets _futureChangesetProtection;
    readonly ILogger<ResolveFutures> _logger;
    readonly IEventSequenceStorage _eventSequenceStorage;
    readonly ISink _sink;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveFutures"/> class.
    /// </summary>
    /// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
    /// <param name="futuresTracker"><see cref="ProjectionFuturesTracker"/> shared with the <see cref="StoreFutures"/> step.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for creating changesets when resolving futures.</param>
    /// <param name="futureChangesetProtection">Protection for resolved future changesets.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> for retrying deferred keys.</param>
    /// <param name="sink"><see cref="ISink"/> for retrying deferred keys.</param>
    public ResolveFutures(
        IProjectionFutures projectionFutures,
        ProjectionFuturesTracker futuresTracker,
        ITypeFormats typeFormats,
        IObjectComparer objectComparer,
        ICanProtectFutureChangesets futureChangesetProtection,
        ILogger<ResolveFutures> logger,
        IEventSequenceStorage eventSequenceStorage,
        ISink sink)
        : this(futuresTracker, new ProjectionFuturesStore(projectionFutures), typeFormats, objectComparer, futureChangesetProtection, logger, eventSequenceStorage, sink)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveFutures"/> class.
    /// </summary>
    /// <param name="futuresTracker"><see cref="ProjectionFuturesTracker"/> shared with the <see cref="StoreFutures"/> step.</param>
    /// <param name="futuresStore"><see cref="IProjectionFuturesStore"/> for managing futures.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for creating changesets when resolving futures.</param>
    /// <param name="futureChangesetProtection">Protection for resolved future changesets.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> for retrying deferred keys.</param>
    /// <param name="sink"><see cref="ISink"/> for retrying deferred keys.</param>
    internal ResolveFutures(
        ProjectionFuturesTracker futuresTracker,
        IProjectionFuturesStore futuresStore,
        ITypeFormats typeFormats,
        IObjectComparer objectComparer,
        ICanProtectFutureChangesets futureChangesetProtection,
        ILogger<ResolveFutures> logger,
        IEventSequenceStorage eventSequenceStorage,
        ISink sink)
    {
        _projectionFutures = futuresStore;
        _futuresTracker = futuresTracker;
        _typeFormats = typeFormats;
        _objectComparer = objectComparer;
        _futureChangesetProtection = futureChangesetProtection;
        _logger = logger;
        _eventSequenceStorage = eventSequenceStorage;
        _sink = sink;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Deferred children use the key and state materialized by the releasing parent event, including that event.
    /// History-based retries use the same inclusive release boundary (see <see cref="ParentKeyResolutionScope"/>).
    /// Ordinary resolution remains strictly before the child; later replay-page events cannot reroute a release.
    /// </remarks>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        // If this event was deferred or key is permanently unresolvable, don't try to resolve futures.
        // Nothing having been stored since resolution last drained the futures store also skips the grain call.
        if (context.IsDeferred || context.IsUnresolvable || !_futuresTracker.HasPending)
        {
            return context;
        }

        // Attempt to resolve any pending futures now that we've processed a new event
        var futures = await _projectionFutures.GetFutures();

        _failedFutures.IntersectWith(futures.Select(future => future.Id));
        if (!futures.Any())
        {
            _futuresTracker.HasPending = false;
            return context;
        }

        _logger.FoundPendingFutures(futures.Count(), projection.Identifier);
        using var resolutionScope = ParentKeyResolutionScope.ForDeferredRelease(context.Event.Context.SequenceNumber);

        var (remaining, latestResolvedEvent) = await ResolveUntilStable(projection, context);

        // Queued writes remain persisted too, so a failed save keeps the tracker pending.
        _futuresTracker.HasPending = remaining.Any();

        return context with { Event = latestResolvedEvent };
    }

    // Keep trying to resolve futures until we can't resolve any more
    // This handles the case where resolving one future creates the parent data needed by another future
    async Task<(IEnumerable<ProjectionFuture> Remaining, AppendedEvent LatestResolvedEvent)> ResolveUntilStable(IProjection projection, ProjectionEventContext context)
    {
        var resolvedAny = true;
        var latestResolvedEvent = context.Event;
        var queuedFutures = new HashSet<ProjectionFutureId>();
        IEnumerable<ProjectionFuture> futures = [];
        while (resolvedAny)
        {
            resolvedAny = false;
            futures = await _projectionFutures.GetFutures();

            foreach (var future in futures.Where(future => !queuedFutures.Contains(future.Id)))
            {
                if (!await TryResolveFuture(projection, context, future, queuedFutures))
                {
                    continue;
                }

                resolvedAny = true;
                if (future.Event.Context.SequenceNumber > latestResolvedEvent.Context.SequenceNumber)
                {
                    latestResolvedEvent = future.Event;
                }
            }
        }

        return (futures, latestResolvedEvent);
    }

    async Task<bool> TryResolveFuture(IProjection projection, ProjectionEventContext context, ProjectionFuture future, HashSet<ProjectionFutureId> queuedFutures)
    {
        try
        {
            var parentKey = ConvertParentKey(projection, future);

            // Find the child projection that this future belongs to by navigating the property path
            var childProjection = FindChildProjectionByPath(projection, future.ChildPath);
            if (childProjection is null)
            {
                LogFailure(new InvalidOperationException($"Could not find child projection for path '{future.ChildPath}'"), future);
                return false;
            }

            _logger.FoundChildProjection(childProjection.Path, future.Id);
            _logger.CheckingParentInCurrentState(future.Id);

            // Check if parent exists in the current changeset's state, collecting the full indexer
            // chain needed to navigate from root to the parent. For parents nested beyond one level,
            // this searches through intermediate array collections to find the matching parent and
            // records the identifier of each intermediate element.
            var (parentFound, parentIndexers, retriedKey) = await FindParentForFuture(context, new FutureTarget(projection, childProjection, future), parentKey);
            if (!parentFound)
            {
                // Parent still doesn't exist, skip this future
                _logger.ParentNotInCurrentState(future.Id);
                return false;
            }

            _logger.ParentExistsInCurrentState(future.Id);
            _logger.ResolvedKeyCallingOnNext(future.ParentKey, future.Id);

            var childKey = ConvertChildKey(projection, future, ChildKeyFor(future, retriedKey));
            await ApplyResolvedFuture(context, new FutureTarget(projection, childProjection, future), new ResolvedParent(parentIndexers, retriedKey, childKey), queuedFutures);
            return true;
        }
        catch (Exception ex)
        {
            LogFailure(ex, future);
            return false;
        }
    }

    object ConvertParentKey(IProjection projection, ProjectionFuture future)
    {
        var parentIdentifierPath = future.ParentPath + future.ParentIdentifiedByProperty;
        var type = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(parentIdentifierPath, _typeFormats);
        var parentKey = future.ParentKey.Value;
        if (type is null || future.ParentPath.IsRoot)
        {
            return parentKey;
        }

        try
        {
            return DecimalValues.ConvertTo(type, parentKey);
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            // Keep original value — Contains() normalizes types during comparison
            return parentKey;
        }
    }

    object? ConvertChildKey(IProjection projection, ProjectionFuture future, object? childKey)
    {
        var childIdentifierPath = future.ChildPath + future.IdentifiedByProperty;
        var childType = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(childIdentifierPath, _typeFormats);
        if (childType is null || childKey is null)
        {
            return childKey;
        }

        try
        {
            return DecimalValues.ConvertTo(childType, childKey);
        }
        catch
        {
            // Keep original value — Contains() normalizes types during comparison
            return childKey;
        }
    }

    async Task ApplyResolvedFuture(ProjectionEventContext context, FutureTarget target, ResolvedParent resolvedParent, HashSet<ProjectionFutureId> queuedFutures)
    {
        var (projection, childProjection, future) = target;
        var (parentIndexers, _, childKey) = resolvedParent;

        // Build the key using the full parent indexer chain plus the child indexer.
        // parentIndexers contains one entry per ancestor level (root → parent), giving
        // EnsurePath everything it needs to navigate deeply nested arrays.
        parentIndexers.Add(new ArrayIndexer(future.ChildPath, future.IdentifiedByProperty, childKey!));
        var key = new Key(context.Key.Value, new ArrayIndexers(parentIndexers));

        // Use a separate changeset so the future's ChildAdded is saved in its own MongoDB
        // operation after the main changeset save, avoiding MongoDB's restriction on
        // pushing to a child of an array element being pushed in the same update.
        var futureChangeset = new Changeset<AppendedEvent, ExpandoObject>(
            _objectComparer,
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

        futureContext = WithFreshKeyMemo(futureContext, context, target, resolvedParent);

        ProvideToAncestors(childProjection, futureContext);
        childProjection.OnNext(futureContext);

        await ProtectFutureChangeset(projection, futureContext);

        await QueueFutureSave(context, new PendingFutureSave(key, futureChangeset) { FutureId = future.Id }, queuedFutures);
        _failedFutures.Remove(future.Id);
        _logger.ResolvedFuture(future.Id, future.ProjectionId);
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

    static object? ChildKeyFor(ProjectionFuture future, ResolvedKey? retriedKey) =>
        retriedKey?.Key.ArrayIndexers.All.Last().Identifier ?? GetChildKey(future);

    static ProjectionEventContext WithFreshKeyMemo(ProjectionEventContext futureContext, ProjectionEventContext context, FutureTarget target, ResolvedParent resolvedParent)
    {
        var future = target.Future;
        var (_, retriedKey, childKey) = resolvedParent;
        if (retriedKey is null)
        {
            return futureContext;
        }

        // A different event needs a fresh key memo, not the releasing parent's cached results.
        var fresh = new ProjectionEventContext(
            futureContext.Key,
            future.Event,
            futureContext.Changeset,
            futureContext.OperationType,
            context.NeedsInitialState,
            retriedKey.JoinKey ?? childKey ?? futureContext.Key.Value)
        {
            GenerationDelivery = context.GenerationDelivery,
            IsNewInstance = context.IsNewInstance
        };
        fresh.MemoizeResolvedKey(target.ChildProjection.GetKeyResolverFor(future.Event.Context.EventType), retriedKey with { Key = futureContext.Key });
        return fresh;
    }

    static bool IsLeafOfFuture(ArrayIndexer[] indexers, ProjectionFuture future)
    {
        if (indexers.Length == 0)
        {
            return false;
        }

        var leaf = indexers[^1];
        return leaf.ArrayProperty == future.ChildPath && leaf.IdentifierProperty == future.IdentifiedByProperty;
    }

    static bool MatchesParentChain(ProjectionEventContext context, FutureTarget target, ResolvedKey resolved, ArrayIndexer[] indexers)
    {
        var (_, childProjection, future) = target;
        var parentKey = future.ParentPath.IsRoot ? resolved.Key.Value : indexers.LastOrDefault(indexer => indexer.ArrayProperty == future.ParentPath)?.Identifier;
        if (parentKey is null)
        {
            return false;
        }

        if (!FutureParentSearch.TryFindParentWithIndexers(ParentSearch.For(childProjection, context, future, parentKey), out var parentIndexers))
        {
            return false;
        }

        // A matching leaf alone is insufficient when the resolver selected a different ancestor chain.
        var expectedParents = indexers.SkipLast(1).ToArray();
        return parentIndexers.Count == expectedParents.Length && parentIndexers.Zip(expectedParents).All(pair => IsSameIndexer(pair.First, pair.Second));
    }

    static bool IsSameIndexer(ArrayIndexer actual, ArrayIndexer expected) =>
        IsSamePosition(actual, expected) && KeyValueEquality.AreEqual(actual.Identifier, expected.Identifier);

    static bool IsSamePosition(ArrayIndexer actual, ArrayIndexer expected) =>
        actual.ArrayProperty == expected.ArrayProperty && actual.IdentifierProperty == expected.IdentifierProperty;

    ValueTask ProtectFutureChangeset(IProjection projection, ProjectionEventContext context) =>
        context.Changeset.HasChanges
            ? _futureChangesetProtection.ProtectFutureChangeset(projection, context)
            : ValueTask.CompletedTask;

    async Task<(bool Found, List<ArrayIndexer> ParentIndexers, ResolvedKey? RetriedKey)> FindParentForFuture(
        ProjectionEventContext context,
        FutureTarget target,
        object parentKey)
    {
        var (_, childProjection, future) = target;
        if (FutureParentSearch.TryFindParentWithIndexers(ParentSearch.For(childProjection, context, future, parentKey), out var parentIndexers))
        {
            return (true, parentIndexers, null);
        }

        var retriedKey = await RetryDeferredKey(context, target);
        return retriedKey is null
            ? (false, parentIndexers, null)
            : (true, retriedKey.Key.ArrayIndexers.All.SkipLast(1).ToList(), retriedKey);
    }

    async Task<ResolvedKey?> RetryDeferredKey(ProjectionEventContext context, FutureTarget target)
    {
        var (_, childProjection, future) = target;
        if (!IsParentEvent(childProjection, context.EventType.Id))
        {
            return null;
        }

        var resolver = childProjection.GetKeyResolverFor(future.Event.Context.EventType);
        if (await resolver(_eventSequenceStorage, _sink, future.Event) is not ResolvedKey resolved ||
            !KeyValueEquality.AreEqual(resolved.Key.Value, context.Key.Value))
        {
            return null;
        }

        var indexers = resolved.Key.ArrayIndexers.All.ToArray();
        return IsLeafOfFuture(indexers, future) && MatchesParentChain(context, target, resolved, indexers) ? resolved : null;
    }

    void LogFailure(Exception exception, ProjectionFuture future)
    {
        if (_failedFutures.Add(future.Id))
        {
            _logger.FailedToResolveFuture(exception, future.Id, future.ProjectionId);
        }
        else
        {
            _logger.FutureStillUnresolved(exception, future.Id, future.ProjectionId);
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
            await _projectionFutures.ResolveFuture(futureId);
        }
    }
}
