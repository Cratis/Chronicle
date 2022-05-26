// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using Orleans;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjections"/>.
/// </summary>
public class ImmediateProjections : IImmediateProjections
{
    static class ImmediateProjectionsCache<TProjection>
    {
        public static TProjection? Instance;
        public static ProjectionDefinition? Definition;
    }

    readonly ITypes _types;
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IClusterClient _clusterClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjections"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for finding types.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for providing projections.</param>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating model schema.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="clusterClient"><see cref="IClusterClient"/> for working with Orleans.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with the execution context.</param>
    public ImmediateProjections(
        ITypes types,
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions,
        IClusterClient clusterClient,
        IExecutionContextManager executionContextManager)
    {
        _types = types;
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes;
        _schemaGenerator = schemaGenerator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _clusterClient = clusterClient;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task<TModel> GetInstanceById<TModel>(EventSourceId eventSourceId)
    {
        HandleProjectionTypeCache<TModel>();

        var key = new ImmediateProjectionKey(_executionContextManager.Current.MicroserviceId, _executionContextManager.Current.TenantId, EventSequenceId.Log, eventSourceId);
        var projection = _clusterClient.GetGrain<IImmediateProjection>(ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance!.Identifier, key);
        var json = await projection.GetModelInstance(ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Definition!);

        return json.Deserialize<TModel>(_jsonSerializerOptions)!;
    }

    void HandleProjectionTypeCache<TModel>()
    {
        if (ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance is null)
        {
            var projectionType = _types.FindSingle<IImmediateProjectionFor<TModel>>();
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance = (_serviceProvider.GetService(projectionType) as IImmediateProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(
                ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance.Identifier,
                _eventTypes,
                _schemaGenerator);

            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance.Define(builder);
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Definition = builder.Build();
        }
    }
}
