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
    readonly ISinks _sinks;
    readonly IObjectComparer _objectComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerPipelineFactory"/> class.
    /// </summary>
    /// <param name="projectionSinks"><see cref="ISinks"/> in the system.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
    public ReducerPipelineFactory(
        ISinks projectionSinks,
        IObjectComparer objectComparer)
    {
        _sinks = projectionSinks;
        _objectComparer = objectComparer;
    }

    /// <inheritdoc/>
    public async Task<IReducerPipeline> CreateFrom(ReducerDefinition definition)
    {
        var readModelSchema = await JsonSchema.FromJsonAsync(definition.ReadModel.Schema);
        var readModel = new Model(definition.ReadModel.Name, readModelSchema);
        var sink = _sinks.GetForTypeAndModel(definition.SinkTypeId, readModel);
        return new ReducerPipeline(readModel, sink, _objectComparer);
    }
}
