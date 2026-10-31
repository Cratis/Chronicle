// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;
using EngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelInstanceVisibilities"/> that follows the projection definition.
/// </summary>
/// <param name="projectionsManager">The engine projections manager used to look up the projection behind a read model.</param>
/// <remarks>
/// The roots of a child-only projection have no creation event of their own, so they stay visible regardless of their
/// stored initialization marker. Every other read model keeps hiding roots stored as uninitialized.
/// </remarks>
[Singleton]
public class ReadModelInstanceVisibilities(EngineProjectionsManager projectionsManager) : IReadModelInstanceVisibilities
{
    /// <inheritdoc/>
    public async Task<ReadModelInstanceVisibility> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition)
    {
        if (definition.ObserverType != ReadModelObserverType.Projection)
        {
            return ReadModelInstanceVisibility.InitializedOnly;
        }

        var id = (ProjectionId)definition.ObserverIdentifier.Value;
        var projection = projectionsManager.TryGet(eventStore, eventStoreNamespace, id, out var registered)
            ? registered
            : await projectionsManager.GetCurrent(eventStore, eventStoreNamespace, id);

        return projection is not null && ProjectionInitialization.IsChildOnly(projection)
            ? ReadModelInstanceVisibility.IncludeUninitialized
            : ReadModelInstanceVisibility.InitializedOnly;
    }
}
