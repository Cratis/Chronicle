// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Represents the <see cref="IModelBoundProjectionsSource"/> that discovers the definitions from the declared model-bound types.
/// </summary>
internal sealed class ModelBoundProjectionsSource : IModelBoundProjectionsSource
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    internal static readonly ModelBoundProjectionsSource Default = new();

    /// <inheritdoc/>
    public (IDictionary<Type, ProjectionDefinition> Definitions, IDictionary<Type, Exception> Failures) Discover(
        IClientArtifactsProvider clientArtifactsProvider,
        INamingPolicy namingPolicy,
        IEventTypes eventTypes,
        ILogger<Projections> logger,
        string? currentEventStoreName)
    {
        var projections = new ModelBoundProjections(clientArtifactsProvider, namingPolicy, eventTypes, logger, currentEventStoreName);
        var definitions = projections.Discover();
        return (definitions, projections.Failures);
    }
}
