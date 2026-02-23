// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Holds log messages for <see cref="ProjectionsManager"/>.
/// </summary>
internal static partial class ProjectionsManagerLogging
{
    [LoggerMessage(LogLevel.Debug, "Setting definition for projection '{Identifier}'")]
    internal static partial void SettingDefinition(this ILogger<ProjectionsManager> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Debug, "Subscribing projection '{Identifier}' in namespace '{Namespace}'")]
    internal static partial void Subscribing(this ILogger<ProjectionsManager> logger, ProjectionId identifier, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Subscribing projection '{Identifier}' with {Count} event types: {EventTypes}")]
    internal static partial void SubscribingWithEventTypes(this ILogger<ProjectionsManager> logger, ProjectionId identifier, int count, string eventTypes);

    [LoggerMessage(LogLevel.Warning, "Read model definition '{ReadModel}' not found for projection '{Identifier}'")]
    internal static partial void MissingReadModelDefinitionForProjection(this ILogger<ProjectionsManager> logger, ProjectionId identifier, ReadModelIdentifier readModel);
}
