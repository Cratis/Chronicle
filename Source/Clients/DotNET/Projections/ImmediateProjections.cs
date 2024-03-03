// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Aksio.Collections;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Kernel.Contracts.Projections;
using Cratis.Models;
using Cratis.Schemas;
using Aksio.Reflection;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjections"/>.
/// </summary>
public class ImmediateProjections : IImmediateProjections
{
    readonly IModelNameResolver _modelNameResolver;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly List<ProjectionDefinition> _definitions = new();
    readonly IDictionary<Type, ProjectionDefinition> _definitionsByModelType = new Dictionary<Type, ProjectionDefinition>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjections"/> class.
    /// </summary>
    /// <param name="modelNameResolver">The <see cref="IModelNameResolver"/> to use for naming the models.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for providing projections.</param>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating model schema.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with the execution context.</param>
    public ImmediateProjections(
        IModelNameResolver modelNameResolver,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager)
    {
        _modelNameResolver = modelNameResolver;
        _clientArtifacts = clientArtifacts;
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _schemaGenerator = schemaGenerator;
        _jsonSerializerOptions = jsonSerializerOptions;
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
    public bool HasProjectionFor(Type modelType) => _definitionsByModelType.ContainsKey(modelType);

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
    public Task<ImmediateProjectionResultRaw> GetInstanceById(ProjectionId identifier, ModelKey modelKey) =>
            GetInstanceByIdImplementation(identifier, modelKey);

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetInstanceByIdForSession(
        CorrelationId correlationId,
        Type modelType,
        ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];
        var result = await GetInstanceByIdImplementation(projectionDefinition.Identifier, modelKey, correlationId);
        var model = result.Model.Deserialize(modelType, _jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetInstanceByIdForSessionWithEventsApplied(
        CorrelationId correlationId,
        Type modelType,
        ModelKey modelKey,
        IEnumerable<object> events)
    {
        var projectionDefinition = _definitionsByModelType[modelType];
        var eventsToApplyTasks = events.Select(async _ =>
            new EventToApply(
                _eventTypes.GetEventTypeFor(_.GetType()),
                await _eventSerializer.Serialize(_))).ToArray();

        var eventsToApply = await Task.WhenAll(eventsToApplyTasks);

        var immediateProjection = new ImmediateProjectionWithEventsToApply(
            projectionDefinition.Identifier,
            EventSequenceId.Log,
            modelKey,
            eventsToApply);

        // var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections/immediate/{_executionContextManager.Current.TenantId}/session/{correlationId}/with-events";

        // var response = await _connection.PerformCommand(route, immediateProjection);
        // var element = (JsonElement)response.Response!;
        // var result = element.Deserialize<ImmediateProjectionResultRaw>(_jsonSerializerOptions)!;

        // var model = result.Model.Deserialize(modelType, _jsonSerializerOptions)!;
        // return new(model, result.AffectedProperties, result.ProjectedEventsCount);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(CorrelationId correlationId, Type modelType, ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];
        var immediateProjection = new ImmediateProjection(
            projectionDefinition.Identifier,
            EventSequenceId.Log,
            modelKey);

        // var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections/immediate/{_executionContextManager.Current.TenantId}/session/{correlationId}/dehydrate";
        // await _connection.PerformCommand(route, immediateProjection);
        throw new NotImplementedException();
    }

    async Task<ImmediateProjectionResultRaw> GetInstanceByIdImplementation(ProjectionId identifier, ModelKey modelKey, CorrelationId? correlationId = default)
    {
        var immediateProjection = new ImmediateProjection(
            identifier,
            EventSequenceId.Log,
            modelKey);

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections/immediate/{_executionContextManager.Current.TenantId}";
        if (correlationId is not null)
        {
            route = $"{route}/session/{correlationId}";
        }

        // var result = await _connection.PerformCommand(route, immediateProjection);
        // var element = (JsonElement)result.Response!;
        // return element.Deserialize<ImmediateProjectionResultRaw>(_jsonSerializerOptions)!;
        throw new NotImplementedException();
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

            var projectionDefinition = builder.Build();
            projectionDefinition.IsActive = false;
            _definitions.Add(projectionDefinition);
            _definitionsByModelType[typeof(TModel)] = projectionDefinition;
        }
    }
}
