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

    [LoggerMessage(LogLevel.Trace, "Subscribing projection '{Identifier}' with {Count} event types: {EventTypes}")]
    internal static partial void SubscribingWithEventTypes(this ILogger<ProjectionsManager> logger, ProjectionId identifier, int count, string eventTypes);

    [LoggerMessage(LogLevel.Warning, "Read model definition '{ReadModel}' not found for projection '{Identifier}'")]
    internal static partial void MissingReadModelDefinitionForProjection(this ILogger<ProjectionsManager> logger, ProjectionId identifier, ReadModelIdentifier readModel);

    [LoggerMessage(LogLevel.Debug, "All projection definitions in the registration are identical to the registered ones - skipping registration work")]
    internal static partial void AllDefinitionsUnchanged(this ILogger<ProjectionsManager> logger);

    [LoggerMessage(LogLevel.Debug, "Registering {Count} changed projection definitions")]
    internal static partial void RegisteringChangedDefinitions(this ILogger<ProjectionsManager> logger, int count);

    [LoggerMessage(LogLevel.Error, "The projection engine rejected the definition for projection '{Identifier}' - its previously registered definition remains in effect and registration will be retried on the next registration")]
    internal static partial void FailedRegisteringProjectionWithEngine(this ILogger<ProjectionsManager> logger, Exception ex, ProjectionId identifier);

    [LoggerMessage(LogLevel.Error, "Failed setting the definition or subscribing the observer for projection '{Identifier}'")]
    internal static partial void FailedSettingDefinitionAndSubscribing(this ILogger<ProjectionsManager> logger, Exception ex, ProjectionId identifier);

    [LoggerMessage(LogLevel.Information, "Retiring projection '{Identifier}' - it is no longer registered by its owner. Its observer is unsubscribed and its definition removed; its sink container is left untouched")]
    internal static partial void RetiringProjection(this ILogger<ProjectionsManager> logger, ProjectionId identifier);

    [LoggerMessage(LogLevel.Information, "Retired projection '{Identifier}' wrote to container '{ContainerName}' which projection '{SuccessorIdentifier}' also targets - recommending a replay of the successor to rebuild the container")]
    internal static partial void RetiredProjectionSharedContainer(this ILogger<ProjectionsManager> logger, ProjectionId identifier, ReadModelContainerName containerName, ProjectionId successorIdentifier);

    [LoggerMessage(LogLevel.Warning, "Failed retiring projection '{Identifier}' - it remains registered and retirement will be retried on the next full registration")]
    internal static partial void FailedRetiringProjection(this ILogger<ProjectionsManager> logger, Exception ex, ProjectionId identifier);
}
