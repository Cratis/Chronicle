// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Holds log messages for <see cref="Projections"/>.
/// </summary>
internal static partial class ProjectionsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering projection '{Name} ({Identifier})'")]
    internal static partial void Registering(this ILogger<Projections> logger, ProjectionId identifier, ProjectionName name);

    [LoggerMessage(1, LogLevel.Information, "Projection '{Name} ({Identifier})' is a new projection")]
    internal static partial void ProjectionIsNew(this ILogger<Projections> logger, ProjectionId identifier, ProjectionName name);

    [LoggerMessage(2, LogLevel.Information, "Registering projection '{Name} ({Identifier})' has changed its definition")]
    internal static partial void ProjectionHasChanged(this ILogger<Projections> logger, ProjectionId identifier, ProjectionName name);

    [LoggerMessage(3, LogLevel.Information, "Rehydrating projections and pipelines")]
    internal static partial void Rehydrate(this ILogger<Projections> logger);
}
