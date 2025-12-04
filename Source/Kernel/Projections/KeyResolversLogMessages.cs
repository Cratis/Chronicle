// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

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

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: found root key by sink query, rootKey='{RootKey}'")]
    internal static partial void FromParentHierarchyFoundRootKeyBySink(this ILogger<KeyResolvers> logger, string rootKey);

    [LoggerMessage(LogLevel.Information, "FromParentHierarchy: creating future for deferred resolution - projection='{Path}', childKey='{ChildKey}'")]
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
}
