// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Holds log messages for <see cref="ProjectionsManager"/>.
/// </summary>
internal static partial class ProjectionLogging
{
    [LoggerMessage(LogLevel.Debug, "Setting projection definition and subscribing for projection '{Identifier}'")]
    internal static partial void SettingDefinition(this ILogger<Projection> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Debug, "Projection '{Identifier}' is a new projection")]
    internal static partial void ProjectionIsNew(this ILogger<Projection> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Debug, "Registering projection '{Identifier}' has changed its definition")]
    internal static partial void ProjectionHasChanged(this ILogger<Projection> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Information, "Full replay - projection '{Identifier}' in namespace '{Namespace}' changed in a way that may affect existing read models")]
    internal static partial void AutoReplayingProjection(this ILogger<Projection> logger, ProjectionId identifier, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Partial replay - projection '{Identifier}' in namespace '{Namespace}' only affects {AffectedEventSourceCount} event sources")]
    internal static partial void PartiallyReplayingProjection(this ILogger<Projection> logger, ProjectionId identifier, EventStoreNamespaceName @namespace, int affectedEventSourceCount);

    [LoggerMessage(LogLevel.Information, "No action - projection '{Identifier}' in namespace '{Namespace}' only consumes newly added event types with no historical events")]
    internal static partial void ProjectionEvolutionNeedsNoAction(this ILogger<Projection> logger, ProjectionId identifier, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Rehydrating projections and pipelines")]
    internal static partial void Rehydrate(this ILogger<Projection> logger);
}
