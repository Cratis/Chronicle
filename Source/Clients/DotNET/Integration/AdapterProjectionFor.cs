// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Shared.Projections;
using Aksio.Cratis.Shared.Projections.Definitions;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class AdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>
{
    readonly ProjectionDefinition _projectionDefinition;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterProjectionFor{TModel}"/> class.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="ProjectionDefinition"/> to work with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public AdapterProjectionFor(
        ProjectionDefinition projectionDefinition,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager)
    {
        _projectionDefinition = projectionDefinition;
        _jsonSerializerOptions = jsonSerializerOptions;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey)
    {
        throw new NotImplementedException();

        // var key = new ImmediateProjectionKey(
        //     _executionContextManager.Current.MicroserviceId,
        //     _executionContextManager.Current.TenantId,
        //     Events.Store.EventSequenceId.Log,
        //     modelKey);
        // var projection = _clusterClient.GetGrain<IImmediateProjection>(_projectionDefinition.Identifier, key);
        // var result = await projection.GetModelInstance(_projectionDefinition);
        // return new(result.Model.Deserialize<TModel>(_jsonSerializerOptions)!, result.AffectedProperties, result.ProjectedEventsCount);
    }
}
