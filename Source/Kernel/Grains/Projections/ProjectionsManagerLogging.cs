// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Holds log messages for <see cref="ProjectionsManager"/>.
/// </summary>
internal static partial class ProjectionsManagerLogging
{
    [LoggerMessage(LogLevel.Information, "Setting definition for projection '{Identifier}'")]
    internal static partial void SettingDefinition(this ILogger<ProjectionsManager> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Information, "Subscribing projection '{Identifier}' in namespace '{Namespace}'")]
    internal static partial void Subscribing(this ILogger<ProjectionsManager> logger, ProjectionId identifier, EventStoreNamespaceName @namespace);
}
