// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Holds log messages for <see cref="ProjectionsManager"/>.
/// </summary>
internal static partial class ProjectionLogging
{
    [LoggerMessage(LogLevel.Information, "Registering projection '{Identifier}'")]
    internal static partial void Registering(this ILogger<Projection> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Information, "Projection '{Identifier}' is a new projection")]
    internal static partial void ProjectionIsNew(this ILogger<Projection> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Information, "Registering projection '{Identifier}' has changed its definition")]
    internal static partial void ProjectionHasChanged(this ILogger<Projection> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Information, "Rehydrating projections and pipelines")]
    internal static partial void Rehydrate(this ILogger<Projection> logger);
}
