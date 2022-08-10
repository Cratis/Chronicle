// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class AdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>
{
    readonly ProjectionDefinition _projectionDefinition;
    readonly IClusterClient _clusterClient;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterProjectionFor{TModel}"/> class.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="IProjection"/> to work with.</param>
    /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    public AdapterProjectionFor(
        ProjectionDefinition projectionDefinition,
        IClusterClient clusterClient,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _projectionDefinition = projectionDefinition;
        _clusterClient = clusterClient;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey)
    {
        // TODO: Fix so that this is not constant values. Multi-tenancy!!
        var key = new ImmediateProjectionKey(ExecutionContextManager.GlobalMicroserviceId, TenantId.Development, Events.Store.EventSequenceId.Log, modelKey);
        var projection = _clusterClient.GetGrain<IImmediateProjection>(_projectionDefinition.Identifier, key);

        var result = await projection.GetModelInstance(_projectionDefinition);
        return new(result.Model.Deserialize<TModel>(_jsonSerializerOptions)!, result.AffectedProperties, result.ProjectedEventsCount);
    }
}
