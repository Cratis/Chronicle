// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Changes;
using Cratis.Kernel.Storage.Sinks;
using Cratis.Models;
using Cratis.Observation.Reducers;
using NJsonSchema;

namespace Cratis.Kernel.Grains.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelines"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerPipelines"/> class.
/// </remarks>
/// <param name="sinks"><see cref="ISinks"/> in the system.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
[SingletonPerMicroserviceAndTenant]
public class ReducerPipelines(
    ISinks sinks,
    IObjectComparer objectComparer) : IReducerPipelines
{
    readonly ConcurrentDictionary<ReducerId, IReducerPipeline> _pipelines = new();

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
        var sink = sinks.GetFor(definition.SinkTypeId, readModel);
        return new ReducerPipeline(readModel, sink, objectComparer);
    }
}
