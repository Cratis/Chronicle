// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines where the model-bound projection definitions of a <see cref="Projections"/> registry come from.
/// </summary>
internal interface IModelBoundProjectionsSource
{
    /// <summary>
    /// Provides the model-bound projection definitions and the failures for the read models that could not be built.
    /// </summary>
    /// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> to discover from.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> for converting names.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> of the owning event store.</param>
    /// <param name="logger">The logger the build failures are reported to.</param>
    /// <param name="currentEventStoreName">The name of the event store the projections belong to.</param>
    /// <returns>Definitions and failures, both owned by the caller and free to mutate.</returns>
    (IDictionary<Type, ProjectionDefinition> Definitions, IDictionary<Type, Exception> Failures) Discover(
        IClientArtifactsProvider clientArtifactsProvider,
        INamingPolicy namingPolicy,
        IEventTypes eventTypes,
        ILogger<Projections> logger,
        string? currentEventStoreName);
}
