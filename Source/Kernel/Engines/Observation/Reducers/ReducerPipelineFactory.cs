// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Observation.Reducers;
using Aksio.Cratis.Projections;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelineFactory"/>.
/// </summary>
public class ReducerPipelineFactory : IReducerPipelineFactory
{
    readonly IProjectionSinks _projectionSinks;
    readonly IObjectComparer _objectComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerPipelineFactory"/> class.
    /// </summary>
    /// <param name="projectionSinks"><see cref="IProjectionSinks"/> in the system.</param>
    /// <param name="objectComparer"></param>
    public ReducerPipelineFactory(
        IProjectionSinks projectionSinks,
        IObjectComparer objectComparer)
    {
        _projectionSinks = projectionSinks;
        _objectComparer = objectComparer;
    }

    /// <inheritdoc/>
    public async Task<IReducerPipeline> CreateFrom(ReducerDefinition definition)
    {
        var readModelSchema = await JsonSchema.FromJsonAsync(definition.ReadModel.Schema);
        var readModel = new Model(definition.ReadModel.Name, readModelSchema);
        var sink = _projectionSinks.GetForTypeAndModel(definition.SinkTypeId, readModel);
        return new ReducerPipeline(readModel, sink, _objectComparer);
    }
}
