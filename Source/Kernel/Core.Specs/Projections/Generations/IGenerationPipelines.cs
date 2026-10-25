// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage.Sinks;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// An abstract twin of the pipeline manager's default-implemented generation overload. The pinned Castle.Core cannot
/// intercept default interface members, so specifications substitute this and route through
/// <see cref="ForwardingPipelineManager"/>.
/// </summary>
public interface IGenerationPipelines
{
    Task<IProjectionPipeline> GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection, GenerationSinkContext context);
}

/// <summary>
/// Forwards abstract members to a substituted manager and the generation overload to <see cref="IGenerationPipelines"/>.
/// </summary>
/// <param name="inner">The substituted manager.</param>
/// <param name="generations">The substituted generation overload.</param>
public class ForwardingPipelineManager(IProjectionPipelineManager inner, IGenerationPipelines generations) : IProjectionPipelineManager
{
    public Task<IProjectionPipeline> GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection) =>
        inner.GetFor(eventStore, @namespace, projection);

    public Task<IProjectionPipeline> GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection, MaterializationGenerationIdentity generation) =>
        inner.GetFor(eventStore, @namespace, projection, generation);

    public Task<IProjectionPipeline> GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection, GenerationSinkContext context) =>
        generations.GetFor(eventStore, @namespace, projection, context);

    public void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id) =>
        inner.EvictFor(eventStore, @namespace, id);

    public void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, MaterializationGenerationIdentity generation) =>
        inner.EvictFor(eventStore, @namespace, generation);

    public void Clear() => inner.Clear();
}
