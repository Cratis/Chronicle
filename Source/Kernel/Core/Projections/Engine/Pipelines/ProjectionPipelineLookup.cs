// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>Resolves live pipeline ownership independently of the compiled-graph cache.</summary>
internal static class ProjectionPipelineLookup
{
    /// <summary>Reuses an existing pipeline and its graph, creating them only if no pipeline exists.</summary>
    /// <param name="projections">The compiled-graph manager.</param>
    /// <param name="pipelines">The live-pipeline manager.</param>
    /// <param name="key">The observer key.</param>
    /// <returns>The pipeline and its actual graph, or null when the projection does not exist.</returns>
    internal static async Task<ProjectionPipelineBinding?> GetFor(IProjectionsManager projections, IProjectionPipelineManager pipelines, ObserverKey key)
    {
        if (pipelines.TryGetFor(key.EventStore, key.Namespace, key.ObserverId, out var binding)) return binding;

        var projection = await projections.GetCurrent(key.EventStore, key.Namespace, key.ObserverId);
        if (projection is null) return null;
        var pipeline = await pipelines.GetFor(key.EventStore, key.Namespace, projection);

        // A concurrent caller may have installed the pipeline first. Return its graph, not our cache miss's graph.
        return pipelines.TryGetFor(key.EventStore, key.Namespace, key.ObserverId, out binding)
            ? binding
            : new(projection, pipeline);
    }
}
