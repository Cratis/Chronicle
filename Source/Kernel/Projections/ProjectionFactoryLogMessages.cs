// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Holds log messages for <see cref="ProjectionFactory"/>.
/// </summary>
internal static partial class ProjectionFactoryLogMessages
{
    [LoggerMessage(LogLevel.Debug, "GetEventTypeWithKeyResolver: eventType={EventType}, hasParent={HasParent}, parentKey={ParentKey}, projectionHasParent={ProjectionHasParent}, parentIdentifiedByProperty={ParentIdentifiedByProperty}")]
    internal static partial void GetEventTypeWithKeyResolverStart(this ILogger<ProjectionFactory> logger, string eventType, bool hasParent, string? parentKey, bool projectionHasParent, string parentIdentifiedByProperty);

    [LoggerMessage(LogLevel.Debug, "GetEventTypeWithKeyResolver: inferring parent key from parent's IdentifiedByProperty, effectiveParentKey={EffectiveParentKey}, isInferredParentKey={IsInferredParentKey}")]
    internal static partial void GetEventTypeWithKeyResolverInferredParentKey(this ILogger<ProjectionFactory> logger, string effectiveParentKey, bool isInferredParentKey);

    [LoggerMessage(LogLevel.Debug, "GetParentKeyResolverFor: key={Key}, useEventSourceIdFallback={UseEventSourceIdFallback}")]
    internal static partial void GetParentKeyResolverFor(this ILogger<ProjectionFactory> logger, string? key, bool useEventSourceIdFallback);

    [LoggerMessage(LogLevel.Debug, "ResolveEventsForProjection: projectionPath={ProjectionPath}, ownEventCount={OwnEventCount}, childCount={ChildCount}")]
    internal static partial void ResolveEventsForProjectionStart(this ILogger<ProjectionFactory> logger, string projectionPath, int ownEventCount, int childCount);

    [LoggerMessage(LogLevel.Debug, "ResolveEventsForProjection: collecting {Count} event types from child {ChildPath}")]
    internal static partial void CollectingEventsFromChild(this ILogger<ProjectionFactory> logger, int count, string childPath);

    [LoggerMessage(LogLevel.Debug, "ResolveEventsForProjection: final event count={FinalCount} for projection {ProjectionPath}")]
    internal static partial void ResolveEventsForProjectionComplete(this ILogger<ProjectionFactory> logger, int finalCount, string projectionPath);
}
