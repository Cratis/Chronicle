// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Schemas;
using Cratis.Collections;
using Cratis.Models;
using Cratis.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Projections"/> class.
/// </remarks>
public class Projections : IProjections
{
    readonly IEventStore _eventStore;
    readonly IEventTypes _eventTypes;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly IModelNameResolver _modelNameResolver;
    readonly IEventSerializer _eventSerializer;
    readonly IServiceProvider _serviceProvider;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly List<ProjectionDefinition> _definitions = [];
    readonly IDictionary<Type, ProjectionDefinition> _definitionsByModelType = new Dictionary<Type, ProjectionDefinition>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> the projections belongs to.</param>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public Projections(
        IEventStore eventStore,
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameResolver modelNameResolver,
        IEventSerializer eventSerializer,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _eventStore = eventStore;
        _eventTypes = eventTypes;
        _clientArtifacts = clientArtifacts;
        _schemaGenerator = schemaGenerator;
        _modelNameResolver = modelNameResolver;
        _eventSerializer = eventSerializer;
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = jsonSerializerOptions;

        _clientArtifacts.Projections.ForEach(_ =>
        {
            var projectionType = _.GetInterfaces().Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IProjectionFor<>));
            GetType()
                .GetMethod(nameof(HandleProjectionTypeCache), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(projectionType.GetGenericArguments()[0])!
                .Invoke(this, null);
        });

        Definitions = _definitions.ToImmutableList();
    }

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; private set; }

    /// <inheritdoc/>
    public bool HasProjectionFor(Type modelType) => _definitionsByModelType.ContainsKey(modelType);

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceById(Type modelType, ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];
        var result = await GetInstanceById(projectionDefinition.Identifier, modelKey);
        var model = result.Model.Deserialize(modelType, _jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[typeof(TModel)];
        var request = new GetInstanceByIdRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStoreName = _eventStore.EventStoreName,
            Namespace = _eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
        };

        var result = await _eventStore.Connection.Services.Projections.GetInstanceById(request);
        return result.ToClient<TModel>();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResultRaw> GetInstanceById(ProjectionId identifier, ModelKey modelKey)
    {
        var projectionDefinition = _definitions.Single(_ => _.Identifier == identifier);
        var request = new GetInstanceByIdRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStoreName = _eventStore.EventStoreName,
            Namespace = _eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
        };

        var result = await _eventStore.Connection.Services.Projections.GetInstanceById(request);
        return result.ToClient();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSession(
        ProjectionSessionId sessionId,
        Type modelType,
        ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];

        var request = new GetInstanceByIdForSessionRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStoreName = _eventStore.EventStoreName,
            Namespace = _eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
            SessionId = sessionId
        };

        var result = await _eventStore.Connection.Services.Projections.GetInstanceByIdForSession(request);
        return result.ToClient(modelType);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(
        ProjectionSessionId sessionId,
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

        var request = new GetInstanceByIdForSessionWithEventsAppliedRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStoreName = _eventStore.EventStoreName,
            Namespace = _eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
            SessionId = sessionId,
            Events = eventsToApply.ToContract()
        };

        var result = await _eventStore.Connection.Services.Projections.GetInstanceByIdFOrSessionWithEventsApplied(request);
        return result.ToClient(modelType);
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(ProjectionSessionId sessionId, Type modelType, ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];

        var request = new DehydrateSessionRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStoreName = _eventStore.EventStoreName,
            Namespace = _eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
            SessionId = sessionId
        };

        await _eventStore.Connection.Services.Projections.DehydrateSession(request);
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        Definitions = FindAllProjectionDefinitions(
            _eventStore.EventTypes,
            _clientArtifacts,
            _schemaGenerator,
            _modelNameResolver,
            _serviceProvider,
            _jsonSerializerOptions).ToImmutableList();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        await _eventStore.Connection.Services.Projections.Register(new()
        {
            EventStoreName = _eventStore.EventStoreName,
            Projections = [.. Definitions]
        });
    }

    void HandleProjectionTypeCache<TModel>()
    {
        var modelType = typeof(TModel);
        if (!_definitionsByModelType.ContainsKey(modelType))
        {
            var projectionType = _clientArtifacts.Projections.Single(_ => _.HasInterface<IProjectionFor<TModel>>())
                ?? throw new MissingImmediateProjectionForModel(modelType);

            var instance = (_serviceProvider.GetService(projectionType) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(
                projectionType.GetProjectionId(),
                _modelNameResolver,
                _eventTypes,
                _schemaGenerator,
                _jsonSerializerOptions);

            instance.Define(builder);

            var projectionDefinition = builder.Build();
            projectionDefinition.IsActive = false;
            _definitions.Add(projectionDefinition);
            _definitionsByModelType[typeof(TModel)] = projectionDefinition;

            Definitions = _definitions.ToImmutableList();
        }
    }

    IEnumerable<ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameResolver modelNameResolver,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions) =>
        clientArtifacts.Projections
                .Select(_ =>
                {
                    var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                    var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                    var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                    return (method.Invoke(
                        null,
                        [
                            _,
                            modelNameResolver,
                            eventTypes,
                            schemaGenerator,
                            serviceProvider,
                            jsonSerializerOptions
                        ]) as ProjectionDefinition)!;
                }).ToArray();

    static class ProjectionDefinitionCreator<TModel>
    {
        public static ProjectionDefinition CreateAndDefine(
            Type type,
            IModelNameResolver modelNameResolver,
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator,
            IServiceProvider serviceProvider,
            JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (serviceProvider.GetRequiredService(type) as IProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(type.GetProjectionId(), modelNameResolver, eventTypes, schemaGenerator, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }
}
