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
}
