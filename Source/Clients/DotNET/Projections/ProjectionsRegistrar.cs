// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Sinks;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IParticipateInConnectionLifecycle"/> for handling registrations of projections with the Kernel.
/// </summary>
public class ProjectionsRegistrar : IParticipateInConnectionLifecycle
{
    static class ProjectionDefinitionCreator<TModel>
    {
        public static ProjectionDefinition CreateAndDefine(Type type, IModelNameResolver modelNameResolver, IEventTypes eventTypes, IJsonSchemaGenerator schemaGenerator, JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (Activator.CreateInstance(type) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(instance.Identifier, modelNameResolver, eventTypes, schemaGenerator, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }

    readonly IEnumerable<ProjectionDefinition> _projections;
    readonly IConnection _connection;
    readonly IExecutionContextManager _executionContextManager;
    readonly IModelNameResolver _modelNameResolver;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<ProjectionsRegistrar> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="connection">The Cratis <see cref="IConnection"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="modelNameResolver">The <see cref="IModelNameResolver"/> to use for naming the models.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serializing projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ProjectionsRegistrar(
        IConnection connection,
        IExecutionContextManager executionContextManager,
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IModelNameResolver modelNameResolver,
        IJsonSchemaGenerator schemaGenerator,
        IJsonProjectionSerializer projectionSerializer,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<ProjectionsRegistrar> logger)
    {
        _connection = connection;
        _executionContextManager = executionContextManager;
        _modelNameResolver = modelNameResolver;
        _projectionSerializer = projectionSerializer;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
        _projections = FindAllProjectionDefinitions(eventTypes, clientArtifacts, schemaGenerator, jsonSerializerOptions);
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
                            WellKnownSinkTypes.MongoDB)
                });
            var serializedPipeline = JsonSerializer.SerializeToNode(pipeline, _jsonSerializerOptions)!;
            return new ProjectionRegistration(
                _projectionSerializer.Serialize(projection),
                serializedPipeline);
        });

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections";
        await _connection.PerformCommand(route, new RegisterProjections(registrations));
    }

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;

    IEnumerable<ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions) =>
        clientArtifacts.Projections
                .Select(_ =>
                {
                    var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                    var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                    var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                    return (method.Invoke(null, new object[] { _, _modelNameResolver, eventTypes, schemaGenerator, jsonSerializerOptions }) as ProjectionDefinition)!;
                }).ToArray();
}
