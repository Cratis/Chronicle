// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

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

    [LoggerMessage(LogLevel.Warning, "Projection '{ProjectionId}' declares root remove via join, which is not supported; no root removal subscription was created.")]
    internal static partial void RootRemovalViaJoinNotSupported(this ILogger<ProjectionFactory> logger, string projectionId);

    [LoggerMessage(LogLevel.Warning, "Projection '{ProjectionId}' declares children inside nested object '{NestedPath}', which is not supported; its child subscriptions were not created.")]
    internal static partial void NestedChildrenNotSupported(this ILogger<ProjectionFactory> logger, string projectionId, string nestedPath);

    [LoggerMessage(LogLevel.Warning, "Projection '{ProjectionId}' declares a join inside nested object '{NestedPath}' under children, which is not supported; its join subscriptions were not created.")]
    internal static partial void NestedJoinInChildrenNotSupported(this ILogger<ProjectionFactory> logger, string projectionId, string nestedPath);

    [LoggerMessage(LogLevel.Warning, "Projection '{ProjectionId}' declares remove via join inside nested object '{NestedPath}', which is not supported; its removal subscriptions were not created.")]
    internal static partial void NestedRemovalViaJoinNotSupported(this ILogger<ProjectionFactory> logger, string projectionId, string nestedPath);

    [LoggerMessage(LogLevel.Warning, "Read model '{ReadModel}' collection property '{Property}' auto-maps to nothing: no property named '{Property}' exists on its source event(s) '{EventTypes}', and it has no explicit mapping, so it will always project as an empty collection. Rename the property to match the event, or map it explicitly with [SetFrom<TEvent>(nameof(...))].")]
    internal static partial void CollectionPropertyAutoMapsToNothing(this ILogger<ProjectionFactory> logger, string readModel, string property, string eventTypes);
}
