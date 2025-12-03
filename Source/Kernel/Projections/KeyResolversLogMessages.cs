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

    [LoggerMessage(LogLevel.Debug, "FromParentHierarchy: final result - parentKey='{ParentKey}', arrayIndexers count={ArrayIndexerCount}")]
    internal static partial void FromParentHierarchyResult(this ILogger<KeyResolvers> logger, object? parentKey, int arrayIndexerCount);
}
