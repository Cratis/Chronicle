// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Models;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Aksio.Reflection;
using Aksio.Schemas;
using Aksio.Types;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IParticipateInClientLifecycle"/> for handling registrations of projections with the Kernel.
/// </summary>
public class ProjectionsRegistrar : IParticipateInClientLifecycle
{
    static class ProjectionDefinitionCreator<TModel>
    {
        public static ProjectionDefinition CreateAndDefine(Type type, IModelNameConvention modelNameConvention, IEventTypes eventTypes, IJsonSchemaGenerator schemaGenerator, JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (Activator.CreateInstance(type) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(instance.Identifier, modelNameConvention, eventTypes, schemaGenerator, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }

    readonly IEnumerable<ProjectionDefinition> _projections;
    readonly IClient _client;
    readonly IExecutionContextManager _executionContextManager;
    readonly IModelNameConvention _modelNameConvention;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<ProjectionsRegistrar> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="client">The Cratis <see cref="IClient"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serializing projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ProjectionsRegistrar(
        IClient client,
        IExecutionContextManager executionContextManager,
        IEventTypes eventTypes,
        ITypes types,
        IModelNameConvention modelNameConvention,
        IJsonSchemaGenerator schemaGenerator,
        IJsonProjectionSerializer projectionSerializer,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<ProjectionsRegistrar> logger)
    {
        _client = client;
        _executionContextManager = executionContextManager;
        _modelNameConvention = modelNameConvention;
        _projectionSerializer = projectionSerializer;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
        _projections = FindAllProjectionDefinitions(eventTypes, types, schemaGenerator, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task ClientConnected()
    {
        _logger.RegisterProjections();

        var registrations = _projections.Select(projection =>
        {
            var pipeline = new ProjectionPipelineDefinition(
                projection.Identifier,
                new[]
                {
                    new ProjectionSinkDefinition(
                            "12358239-a120-4392-96d4-2b48271b904c",
                            WellKnownProjectionSinkTypes.MongoDB)
                });
            var serializedPipeline = JsonSerializer.SerializeToNode(pipeline, _jsonSerializerOptions)!;
            return new ProjectionRegistration(
                _projectionSerializer.Serialize(projection),
                serializedPipeline);
        });

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections";
        await _client.PerformCommand(route, new RegisterProjections(registrations));
    }

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;

    IEnumerable<ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        ITypes types,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions) =>
        types.All
                .Where(_ => _.HasInterface(typeof(IProjectionFor<>)))
                .Select(_ =>
                {
                    var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                    var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                    var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                    return (method.Invoke(null, new object[] { _, _modelNameConvention, eventTypes, schemaGenerator, jsonSerializerOptions }) as ProjectionDefinition)!;
                }).ToArray();
}
