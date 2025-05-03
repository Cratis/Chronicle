// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Schemas;
using Cratis.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Projections"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> the projections belongs to.</param>
/// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
/// <param name="projectionWatcherManager"><see cref="IProjectionWatcherManager"/> for managing watchers.</param>
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="rulesProjections"><see cref="IRulesProjections"/> for getting projection definitions related to rules.</param>
/// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
/// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class Projections(
    IEventStore eventStore,
    IEventTypes eventTypes,
    IProjectionWatcherManager projectionWatcherManager,
    IClientArtifactsProvider clientArtifacts,
    IRulesProjections rulesProjections,
    IJsonSchemaGenerator schemaGenerator,
    IModelNameResolver modelNameResolver,
    IEventSerializer eventSerializer,
    IServiceProvider serviceProvider,
    JsonSerializerOptions jsonSerializerOptions) : IProjections
{
    IDictionary<Type, ProjectionDefinition> _definitionsByModelType = new Dictionary<Type, ProjectionDefinition>();

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; private set; } = ImmutableList<ProjectionDefinition>.Empty;

    /// <inheritdoc/>
    public bool HasFor(ProjectionId projectionId) => Definitions.Any(_ => _.Identifier == projectionId);

    /// <inheritdoc/>
    public bool HasFor(Type modelType) => _definitionsByModelType.ContainsKey(modelType);

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdForModel<TModelType>() => GetProjectionIdForModel(typeof(TModelType));

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdForModel(Type modelType) => _definitionsByModelType[modelType].Identifier;

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceById(Type modelType, ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];
        var result = await GetInstanceById(projectionDefinition.Identifier, modelKey);
        var model = result.Model.Deserialize(modelType, jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[typeof(TModel)];
        var request = new GetInstanceByIdRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
        };

        var result = await eventStore.Connection.Services.Projections.GetInstanceById(request);
        return result.ToClient<TModel>();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResultRaw> GetInstanceById(ProjectionId identifier, ModelKey modelKey)
    {
        var projectionDefinition = Definitions.Single(_ => _.Identifier == identifier);
        var request = new GetInstanceByIdRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
        };

        var result = await eventStore.Connection.Services.Projections.GetInstanceById(request);
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
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
            SessionId = sessionId
        };

        var result = await eventStore.Connection.Services.Projections.GetInstanceByIdForSession(request);
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
                eventTypes.GetEventTypeFor(_.GetType()),
                await eventSerializer.Serialize(_))).ToArray();

        var eventsToApply = await Task.WhenAll(eventsToApplyTasks);

        var request = new GetInstanceByIdForSessionWithEventsAppliedRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
            SessionId = sessionId,
            Events = eventsToApply.ToContract()
        };

        var result = await eventStore.Connection.Services.Projections.GetInstanceByIdForSessionWithEventsApplied(request);
        return result.ToClient(modelType);
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(ProjectionSessionId sessionId, Type modelType, ModelKey modelKey)
    {
        var projectionDefinition = _definitionsByModelType[modelType];

        var request = new DehydrateSessionRequest
        {
            ProjectionId = projectionDefinition.Identifier,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ModelKey = modelKey,
            SessionId = sessionId
        };

        await eventStore.Connection.Services.Projections.DehydrateSession(request);
    }

    /// <inheritdoc/>
    public IObservable<ProjectionChangeset<TModel>> Watch<TModel>() => projectionWatcherManager.GetWatcher<TModel>().Observable;

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitions<TProjection, TModel>()
        where TProjection : IProjectionFor<TModel> =>
            GetFailedPartitions(typeof(TProjection));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitions(Type projectionType)
    {
        var definition = _definitionsByModelType[projectionType];
        return eventStore.FailedPartitions.GetFailedPartitionsFor(definition.Identifier);
    }

    /// <inheritdoc/>
    public async Task<ProjectionState> GetState<TProjection, TModel>()
        where TProjection : IProjectionFor<TModel>
    {
        var projectionType = typeof(TProjection);
        var definition = _definitionsByModelType[projectionType];
        var request = new GetObserverInformationRequest
        {
            ObserverId = definition.Identifier,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = definition.EventSequenceId
        };
        var state = await eventStore.Connection.Services.Observers.GetObserverInformation(request);
        return new ProjectionState(
            state.RunningState.ToClient(),
            state.IsSubscribed,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        _definitionsByModelType = FindAllProjectionDefinitions(
            eventTypes,
            clientArtifacts,
            schemaGenerator,
            modelNameResolver,
            serviceProvider,
            jsonSerializerOptions);

        Definitions =
            ((IEnumerable<ProjectionDefinition>)[
                .. rulesProjections.Discover(),
                .. _definitionsByModelType.Values.ToList()
            ]).ToImmutableList();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        await eventStore.Connection.Services.Projections.Register(new()
        {
            EventStore = eventStore.Name,
            Projections = [.. Definitions]
        });
    }

    static Dictionary<Type, ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IJsonSchemaGenerator schemaGenerator,
        IModelNameResolver modelNameResolver,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions) =>
        clientArtifacts.Projections
                .ToDictionary(
                    _ => _.GetReadModelType(),
                    _ =>
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
                    });

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
