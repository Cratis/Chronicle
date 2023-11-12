// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Aksio.Collections;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;
using Aksio.Cratis.Schemas;
using Aksio.Reflection;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjections"/>.
/// </summary>
public class ImmediateProjections : IImmediateProjections
{
    readonly IModelNameResolver _modelNameResolver;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly IConnection _connection;
    readonly List<ProjectionDefinition> _definitions = new();
    readonly IDictionary<Type, ProjectionDefinition> _definitionsByModelType = new Dictionary<Type, ProjectionDefinition>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjections"/> class.
    /// </summary>
    /// <param name="modelNameResolver">The <see cref="IModelNameResolver"/> to use for naming the models.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for providing projections.</param>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating model schema.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="projectionSerializer">The <see cref="IJsonProjectionSerializer"/> for serializing projection definitions.</param>
    /// <param name="connection">The <see cref="IConnection"/> for connecting to the kernel.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with the execution context.</param>
    public ImmediateProjections(
        IModelNameResolver modelNameResolver,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions,
        IJsonProjectionSerializer projectionSerializer,
        IConnection connection,
        IExecutionContextManager executionContextManager)
    {
        _modelNameResolver = modelNameResolver;
        _clientArtifacts = clientArtifacts;
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes;
        _schemaGenerator = schemaGenerator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _projectionSerializer = projectionSerializer;
        _connection = connection;
        _executionContextManager = executionContextManager;

        _clientArtifacts.ImmediateProjections.ForEach(_ =>
        {
            var immediateProjectionType = _.GetInterfaces().Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IImmediateProjectionFor<>));
            GetType()
                .GetMethod(nameof(HandleProjectionTypeCache), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(immediateProjectionType.GetGenericArguments()[0])!
                .Invoke(this, null);
        });
        Definitions = _definitions.ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetInstanceById(Type modelType, ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];
        var result = await GetInstanceById(projectionDefinition.Identifier, modelKey);
        var model = result.Model.Deserialize(modelType, _jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[typeof(TModel)];
        var result = await GetInstanceById(
            projectionDefinition.Identifier,
            modelKey);

        var model = result.Model.Deserialize<TModel>(_jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResultRaw> GetInstanceById(ProjectionId identifier, ModelKey modelKey)
    {
        var immediateProjection = new ImmediateProjection(
            identifier,
            EventSequenceId.Log,
            modelKey);

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections/immediate/{_executionContextManager.Current.TenantId}";

        var result = await _connection.PerformCommand(route, immediateProjection);
        var element = (JsonElement)result.Response!;
        return element.Deserialize<ImmediateProjectionResultRaw>(_jsonSerializerOptions)!;
    }

    void HandleProjectionTypeCache<TModel>()
    {
        var modelType = typeof(TModel);
        if (!_definitionsByModelType.ContainsKey(modelType))
        {
            var projectionType = _clientArtifacts.ImmediateProjections.Single(_ => _.HasInterface<IImmediateProjectionFor<TModel>>())
                ?? throw new MissingImmediateProjectionForModel(modelType);

            var instance = (_serviceProvider.GetService(projectionType) as IImmediateProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(
                instance.Identifier,
                _modelNameResolver,
                _eventTypes,
                _schemaGenerator,
                _jsonSerializerOptions);

            instance.Define(builder);

            var projectionDefinition = builder.Build() with { IsActive = false };
            _definitions.Add(projectionDefinition);
            _definitionsByModelType[typeof(TModel)] = projectionDefinition;
        }
    }
}
