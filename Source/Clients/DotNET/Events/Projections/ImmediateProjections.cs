// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Shared.EventSequences;
using Aksio.Cratis.Shared.Projections;
using Aksio.Cratis.Shared.Projections.Definitions;
using Aksio.Cratis.Shared.Projections.Json;
using Aksio.Cratis.Types;

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
        public static JsonNode? DefinitionAsJson;
    }

    readonly ITypes _types;
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly IClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjections"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for finding types.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for providing projections.</param>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating model schema.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="projectionSerializer">The <see cref="IJsonProjectionSerializer"/> for serializing projection definitions.</param>
    /// <param name="client">The <see cref="IClient"/> for connecting to the kernel.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with the execution context.</param>
    public ImmediateProjections(
        ITypes types,
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions,
        IJsonProjectionSerializer projectionSerializer,
        IClient client,
        IExecutionContextManager executionContextManager)
    {
        _types = types;
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes;
        _schemaGenerator = schemaGenerator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _projectionSerializer = projectionSerializer;
        _client = client;
        _executionContextManager = executionContextManager;
    }


    /// <inheritdoc/>
    public Task<JsonNode> GetInstanceById(ModelKey modelKey, ProjectionDefinition projectionDefinition)
    {
        var projectionDefinitionAsJson = _projectionSerializer.Serialize(projectionDefinition);
        return GetInstanceById(projectionDefinition.Identifier, modelKey, projectionDefinitionAsJson);
    }

    /// <inheritdoc/>
    public async Task<TModel> GetInstanceById<TModel>(ModelKey modelKey)
    {
        HandleProjectionTypeCache<TModel>();

        var node = await GetInstanceById(
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance!.Identifier,
            modelKey,
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.DefinitionAsJson!);

        return node.Deserialize<TModel>(_jsonSerializerOptions)!;
    }

    async Task<JsonNode> GetInstanceById(ProjectionId identifier, ModelKey modelKey, JsonNode projectionDefinition)
    {
        var immediateProjection = new ImmediateProjection(
            identifier,
            EventSequenceId.Log,
            modelKey,
            projectionDefinition);

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections/immediate/{_executionContextManager.Current.TenantId}";

        var result = await _client.PerformCommand(route, immediateProjection);
        var element = (JsonElement)result.Response!;
        var actualResult = element.Deserialize<ImmediateProjectionResult>(_jsonSerializerOptions)!;
        return actualResult.Model;
    }

    void HandleProjectionTypeCache<TModel>()
    {
        if (ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance is null)
        {
            var projectionType = _types.FindSingle<IImmediateProjectionFor<TModel>>();
            if (projectionType is null)
            {
                throw new MissingImmediateProjectionForModel(typeof(TModel));
            }

            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance = (_serviceProvider.GetService(projectionType) as IImmediateProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(
                ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance.Identifier,
                _eventTypes,
                _schemaGenerator,
                _jsonSerializerOptions);

            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance.Define(builder);
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Definition = builder.Build();
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.DefinitionAsJson = _projectionSerializer.Serialize(ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Definition);
        }
    }
}
