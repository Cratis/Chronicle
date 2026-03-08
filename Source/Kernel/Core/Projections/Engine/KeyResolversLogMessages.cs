// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Holds log messages for <see cref="KeyResolvers"/>.
/// </summary>
internal static partial class KeyResolversLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Resolving key using key resolver: {KeyResolverName}")]
    internal static partial void ResolvingKey(this ILogger<KeyResolvers> logger, string keyResolverName);

    [LoggerMessage(LogLevel.Warning, "An error occurred while resolving the key resolver: {KeyResolverName}")]
    internal static partial void ErrorResolving(this ILogger<KeyResolvers> logger, Exception ex, string keyResolverName);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: ENTRY - eventType='{EventType}', eventSourceId='{EventSourceId}', sequenceNumber={SequenceNumber}")]
    internal static partial void FromParentHierarchyEntry(this ILogger<KeyResolvers> logger, string eventType, string eventSourceId, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: projection path '{Path}', hasParent={HasParent}, parentKey='{ParentKey}'")]
    internal static partial void FromParentHierarchyStart(this ILogger<KeyResolvers> logger, string path, bool hasParent, object? parentKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: resolved child key='{Key}', childrenPropertyPath='{ChildrenPropertyPath}', identifiedByProperty='{IdentifiedByProperty}'")]
    internal static partial void FromParentHierarchyChildKey(this ILogger<KeyResolvers> logger, object? key, string childrenPropertyPath, string identifiedByProperty);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: parentEventTypeIds count={Count}, eventTypeIds=[{EventTypeIds}]")]
    internal static partial void FromParentHierarchyParentEventTypes(this ILogger<KeyResolvers> logger, int count, string eventTypeIds);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: looking up parent event with parentKey='{ParentKey}' for event types")]
    internal static partial void FromParentHierarchyLookupParentEvent(this ILogger<KeyResolvers> logger, string parentKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: found parent event of type '{EventType}', resolving key using parent's key resolver")]
    internal static partial void FromParentHierarchyFoundParentEvent(this ILogger<KeyResolvers> logger, string eventType);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: parent event lookup returned no event for parentKey='{ParentKey}'")]
    internal static partial void FromParentHierarchyNoParentEventFound(this ILogger<KeyResolvers> logger, string parentKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: looking up root key by sink query with childPropertyPath='{ChildPropertyPath}', parentKey='{ParentKey}'")]
    internal static partial void FromParentHierarchyLookupBySink(this ILogger<KeyResolvers> logger, string childPropertyPath, string parentKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: found root key in current state, rootKey='{RootKey}'")]
    internal static partial void FromParentHierarchyFoundRootKeyInCurrentState(this ILogger<KeyResolvers> logger, string rootKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: found root key by sink query, rootKey='{RootKey}'")]
    internal static partial void FromParentHierarchyFoundRootKeyBySink(this ILogger<KeyResolvers> logger, string rootKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: creating future for deferred resolution - projection='{Path}', childKey='{ChildKey}'")]
    internal static partial void FromParentHierarchyCreatingFuture(this ILogger<KeyResolvers> logger, string path, string childKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: final result - parentKey='{ParentKey}', arrayIndexers count={ArrayIndexerCount}")]
    internal static partial void FromParentHierarchyResult(this ILogger<KeyResolvers> logger, object? parentKey, int arrayIndexerCount);

    [LoggerMessage(LogLevel.Debug, "FromEventValueProviderWithFallbackToEventSourceId: eventValueProvider returned key='{Key}', will use fallback={WillUseFallback}")]
    internal static partial void FromEventValueProviderWithFallback(this ILogger<KeyResolvers> logger, object? key, bool willUseFallback);

    [LoggerMessage(LogLevel.Debug, "ResolveParentHierarchyFromSink: starting with currentKey='{CurrentKey}', intermediateKeyValue='{IntermediateKeyValue}', leafChildrenProperty='{LeafChildrenProperty}', leafIdentifiedByProperty='{LeafIdentifiedByProperty}', leafKeyValue='{LeafKeyValue}'")]
    internal static partial void ResolveParentHierarchyFromSinkStart(this ILogger<KeyResolvers> logger, object? currentKey, object? intermediateKeyValue, string leafChildrenProperty, string leafIdentifiedByProperty, object? leafKeyValue);

    [LoggerMessage(LogLevel.Debug, "ResolveParentHierarchyFromSink: collected {Count} indexers, final result with root key='{RootKey}'")]
    internal static partial void ResolveParentHierarchyFromSinkResult(this ILogger<KeyResolvers> logger, int count, object? rootKey);

    [LoggerMessage(LogLevel.Debug, "CollectParentIndexers: starting with projection path='{Path}', currentKey='{CurrentKey}', childKeyValue='{ChildKeyValue}'")]
    internal static partial void CollectParentIndexersStart(this ILogger<KeyResolvers> logger, string path, object? currentKey, object? childKeyValue);

    [LoggerMessage(LogLevel.Debug, "CollectParentIndexers: projection hasParent={HasParent}, parent.ChildrenPropertyPath.IsSet={ChildrenPropertyPathIsSet}")]
    internal static partial void CollectParentIndexersProjectionInfo(this ILogger<KeyResolvers> logger, bool hasParent, bool childrenPropertyPathIsSet);

    [LoggerMessage(LogLevel.Debug, "CollectParentIndexers: looking up parent with childPropertyPath='{ChildPropertyPath}', currentKey='{CurrentKey}'")]
    internal static partial void CollectParentIndexersLookup(this ILogger<KeyResolvers> logger, string childPropertyPath, object? currentKey);

    [LoggerMessage(LogLevel.Debug, "CollectParentIndexers: found root key='{RootKey}', recursing to collect parent's indexers")]
    internal static partial void CollectParentIndexersFoundRoot(this ILogger<KeyResolvers> logger, object? rootKey);

    [LoggerMessage(LogLevel.Debug, "CollectParentIndexers: no root key found via sink, stopping recursion")]
    internal static partial void CollectParentIndexersNoRootFound(this ILogger<KeyResolvers> logger);

    [LoggerMessage(LogLevel.Debug, "CollectParentIndexers: adding indexer for childrenProperty='{ChildrenProperty}', identifiedByProperty='{IdentifiedByProperty}', childKeyValue='{ChildKeyValue}'")]
    internal static partial void CollectParentIndexersAddingIndexer(this ILogger<KeyResolvers> logger, string childrenProperty, string identifiedByProperty, object? childKeyValue);

    [LoggerMessage(LogLevel.Debug, "CollectParentIndexers: completed with {Count} indexers collected")]
    internal static partial void CollectParentIndexersCompleted(this ILogger<KeyResolvers> logger, int count);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: found parent event after trying {TriedCount} of {TotalCount} candidate events")]
    internal static partial void FromParentHierarchyFoundParentEventAfterTrying(this ILogger<KeyResolvers> logger, int triedCount, int totalCount);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: skipping deferred parent event at sequence number {SequenceNumber}")]
    internal static partial void FromParentHierarchySkippingDeferredParentEvent(this ILogger<KeyResolvers> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: FindParentEvent START - eventType='{EventType}', eventSourceId='{EventSourceId}', parentKey='{ParentKey}'")]
    internal static partial void FromParentHierarchyFindParentEventStart(this ILogger<KeyResolvers> logger, string eventType, string eventSourceId, string parentKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: event type '{EventType}' matches parent type, using current event")]
    internal static partial void FromParentHierarchyEventIsParentType(this ILogger<KeyResolvers> logger, string eventType);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: TryGetLastInstanceOfAny with eventSourceId='{EventSourceId}', eventTypes=[{EventTypes}]")]
    internal static partial void FromParentHierarchyTryGetLastInstance(this ILogger<KeyResolvers> logger, string eventSourceId, string eventTypes);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: TryGetLastInstanceOfAny returned no event")]
    internal static partial void FromParentHierarchyNoEventFoundByTryGetLast(this ILogger<KeyResolvers> logger);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: TryGetLastInstanceOfAny found event type='{EventType}', sequence={SequenceNumber}")]
    internal static partial void FromParentHierarchyFoundEventByTryGetLast(this ILogger<KeyResolvers> logger, string eventType, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: resolving last event type='{EventType}', sequence={SequenceNumber}")]
    internal static partial void FromParentHierarchyResolvingLastEvent(this ILogger<KeyResolvers> logger, string eventType, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: last event resolved to key='{Key}'")]
    internal static partial void FromParentHierarchyLastEventResolved(this ILogger<KeyResolvers> logger, string key);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: last event type '{EventType}' not found in parent projection event types")]
    internal static partial void FromParentHierarchyLastEventTypeNotFound(this ILogger<KeyResolvers> logger, string eventType);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: falling back to Sink lookup")]
    internal static partial void FromParentHierarchyFallingBackToSink(this ILogger<KeyResolvers> logger);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: TryFindByFallback START - parentProjectionPath='{ParentProjectionPath}', parentKey='{ParentKey}'")]
    internal static partial void FromParentHierarchyTryFindByFallbackStart(this ILogger<KeyResolvers> logger, string parentProjectionPath, string parentKey);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: parent projection '{ProjectionPath}' has no IdentifiedByProperty, deferring")]
    internal static partial void FromParentHierarchyNoParentIdentifiedBy(this ILogger<KeyResolvers> logger, string projectionPath);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: querying Sink with childPropertyPath='{ChildPropertyPath}', parentKeyValue='{ParentKeyValue}'")]
    internal static partial void FromParentHierarchySinkQuery(this ILogger<KeyResolvers> logger, string childPropertyPath, string parentKeyValue);

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: Sink did NOT find root for childPropertyPath='{ChildPropertyPath}', parentKeyValue='{ParentKeyValue}' - deferring")]
    internal static partial void FromParentHierarchySinkDidNotFindRoot(this ILogger<KeyResolvers> logger, string childPropertyPath, string parentKeyValue);
}
