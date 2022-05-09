// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjections"/>.
/// </summary>
public class ImmediateProjections : IImmediateProjections
{
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjections"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with the execution context.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to use for activating grains.</param>
    public ImmediateProjections(
        IExecutionContextManager executionContextManager,
        JsonSerializerOptions jsonSerializerOptions,
        IGrainFactory grainFactory)
    {
        _executionContextManager = executionContextManager;
        _jsonSerializerOptions = jsonSerializerOptions;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public async Task<TModel> GetInstanceById<TModel>(IImmediateProjectionFor<TModel> projectionDefinition, EventSourceId eventSourceId)
    {
        var key = new ImmediateProjectionKey(_executionContextManager.Current.MicroserviceId, _executionContextManager.Current.TenantId, EventSequenceId.Log, eventSourceId);
        var projection = _grainFactory.GetGrain<IImmediateProjection>(projectionDefinition.Identifier, key);
        var json = await projection.GetModelInstance();

        // TODO: Fix the places we manually configures serializer options
        return json.Deserialize<TModel>(_jsonSerializerOptions)!;
    }
}
