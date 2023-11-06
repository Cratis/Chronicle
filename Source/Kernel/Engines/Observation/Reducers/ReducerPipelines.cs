// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Observation.Reducers;
using Aksio.Cratis.Projections;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelines"/>.
/// </summary>
[SingletonPerMicroserviceAndTenant]
public class ReducerPipelines : IReducerPipelines
{
    readonly ISinks _sinks;
    readonly IObjectComparer _objectComparer;
    readonly ConcurrentDictionary<ReducerId, IReducerPipeline> _pipelines = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerPipelines"/> class.
    /// </summary>
    /// <param name="projectionSinks"><see cref="ISinks"/> in the system.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
    public ReducerPipelines(
        ISinks projectionSinks,
        IObjectComparer objectComparer)
    {
        _sinks = projectionSinks;
        _objectComparer = objectComparer;
    }

    /// <inheritdoc/>
    public async Task<IReducerPipeline> GetFor(ReducerDefinition definition)
    {
        if (!_pipelines.TryGetValue(definition.ReducerId, out var pipeline))
        {
            pipeline = await CreatePipeline(definition);
            _pipelines[definition.ReducerId] = pipeline;
        }
        return pipeline;
    }

    async Task<IReducerPipeline> CreatePipeline(ReducerDefinition definition)
    {
        var readModelSchema = await JsonSchema.FromJsonAsync(definition.ReadModel.Schema);
        var readModel = new Model(definition.ReadModel.Name, readModelSchema);
        var sink = _sinks.GetForTypeAndModel(definition.SinkTypeId, readModel);
        return new ReducerPipeline(readModel, sink, _objectComparer);
    }
}
