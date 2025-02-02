// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Log messages for <see cref="ProjectionDefinitionComparer"/>.
/// </summary>
internal static partial class ProjectionDefinitionComparerLogging
{
    [LoggerMessage(LogLevel.Debug, "Projection {ProjectionId} is new, no need to compare definitions")]
    internal static partial void ProjectionIsNew(this ILogger<ProjectionDefinitionComparer> logger, ProjectionId projectionId);

    [LoggerMessage(LogLevel.Debug, "Comparing definitions for Projection {ProjectionId}")]
    internal static partial void ComparingDefinitions(this ILogger<ProjectionDefinitionComparer> logger, ProjectionId projectionId);
}
