// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Serialization;
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
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class Projections(
    IEventStore eventStore,
    IEventTypes eventTypes,
    IProjectionWatcherManager projectionWatcherManager,
    IClientArtifactsProvider clientArtifacts,
    INamingPolicy namingPolicy,
    IEventSerializer eventSerializer,
    IServiceProvider serviceProvider,
    JsonSerializerOptions jsonSerializerOptions) : IProjections
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
    Dictionary<Type, IProjectionHandler> _handlersByType = new();
    Dictionary<Type, IProjectionHandler> _handlersByModelType = new();
    Dictionary<Type, ProjectionDefinition> _definitionsByType = new();

    /// <summary>
    /// Gets all the <see cref="ProjectionDefinition">projection definitions</see>.
    /// </summary>
    internal IImmutableList<ProjectionDefinition> Definitions { get; private set; } = ImmutableList<ProjectionDefinition>.Empty;

    /// <inheritdoc/>
    public bool HasFor(ProjectionId projectionId) => Definitions.Any(_ => _.Identifier == projectionId);

    /// <inheritdoc/>
    public bool HasFor(Type readModelType) => _handlersByModelType.ContainsKey(readModelType);

    /// <inheritdoc/>
    public IEnumerable<IProjectionHandler> GetAllHandlers() => _handlersByModelType.Values;

    /// <inheritdoc/>
    public IProjectionHandler GetHandlerFor<TProjection>()
        where TProjection : IProjection => _handlersByType[typeof(TProjection)];

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdFor<TProjection>()
        where TProjection : IProjection => _handlersByType[typeof(TProjection)].Id;

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdForModel<TReadModel>() => GetProjectionIdForModel(typeof(TReadModel));

    /// <inheritdoc/>
    public ProjectionId GetProjectionIdForModel(Type readModelType) => _handlersByModelType[readModelType].Id;

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceById(Type readModelType, ReadModelKey readModelKey)
    {
        var handler = _handlersByModelType[readModelType];
        var result = await GetInstanceById(handler.Id, readModelKey);
        var model = result.ReadModel.Deserialize(readModelType, jsonSerializerOptions)!;
        return new(model, result.ProjectedEventsCount, result.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult<TReadModel>> GetInstanceById<TReadModel>(ReadModelKey modelKey)
    {
        var handler = _handlersByModelType[typeof(TReadModel)];
        var request = new GetInstanceByIdRequest
        {
            ProjectionId = handler.Id,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = modelKey,
        };

        var result = await _servicesAccessor.Services.Projections.GetInstanceById(request);
        return result.ToClient<TReadModel>(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResultRaw> GetInstanceById(ProjectionId identifier, ReadModelKey readModelKey)
    {
        var handler = Definitions.Single(_ => _.Identifier == identifier);
        var request = new GetInstanceByIdRequest
        {
            ProjectionId = handler.Identifier,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = readModelKey,
        };

        var result = await _servicesAccessor.Services.Projections.GetInstanceById(request);
        return result.ToClient();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSession(
        ProjectionSessionId sessionId,
        Type readModelType,
        ReadModelKey readModelKey)
    {
        var handler = _handlersByModelType[readModelType];

        var request = new GetInstanceByIdForSessionRequest
        {
            ProjectionId = handler.Id,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = readModelKey,
            SessionId = sessionId
        };

        var result = await _servicesAccessor.Services.Projections.GetInstanceByIdForSession(request);
        return result.ToClient(readModelType, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(
        ProjectionSessionId sessionId,
        Type readModelType,
        ReadModelKey readModelKey,
        IEnumerable<object> events)
    {
        var handler = _handlersByModelType[readModelType];
        var eventsToApplyTasks = events.Select(async _ =>
            new EventToApply(
                eventTypes.GetEventTypeFor(_.GetType()),
                await eventSerializer.Serialize(_))).ToArray();

        var eventsToApply = await Task.WhenAll(eventsToApplyTasks);

        var request = new GetInstanceByIdForSessionWithEventsAppliedRequest
        {
            ProjectionId = handler.Id,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = readModelKey,
            SessionId = sessionId,
            Events = eventsToApply.ToContract(jsonSerializerOptions)
        };

        var result = await _servicesAccessor.Services.Projections.GetInstanceByIdForSessionWithEventsApplied(request);
        return result.ToClient(readModelType, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(ProjectionSessionId sessionId, Type readModelType, ReadModelKey readModelKey)
    {
        var handler = _handlersByModelType[readModelType];
        var request = new DehydrateSessionRequest
        {
            ProjectionId = handler.Id,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = readModelKey,
            SessionId = sessionId
        };

        await _servicesAccessor.Services.Projections.DehydrateSession(request);
    }

    /// <inheritdoc/>
    public IObservable<ProjectionChangeset<TReadModel>> Watch<TReadModel>() => projectionWatcherManager.GetWatcher<TReadModel>().Observable;

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor<TProjection>()
        where TProjection : IProjection =>
            GetFailedPartitionsFor(typeof(TProjection));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsFor(Type projectionType)
    {
        var handler = _handlersByModelType[projectionType];
        return handler.GetFailedPartitions();
    }

    /// <inheritdoc/>
    public Task<ProjectionState> GetStateFor<TProjection>()
        where TProjection : IProjection
    {
        var projectionType = typeof(TProjection);
        var handler = _handlersByModelType[projectionType];
        return handler.GetState();
    }

    /// <inheritdoc/>
    public Task Replay<TProjection>()
        where TProjection : IProjection
    {
        var projectionType = typeof(TProjection);
        var handler = _handlersByType[projectionType];
        return Replay(handler.Id);
    }

    /// <inheritdoc/>
    public Task Replay(ProjectionId projectionId)
    {
        return _servicesAccessor.Services.Observers.Replay(new Contracts.Observation.Replay
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ObserverId = projectionId,
            EventSequenceId = string.Empty
        });
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        var modelBoundProjections = new ModelBoundProjections(clientArtifacts, namingPolicy, eventTypes);
        var modelBoundDefinitions = modelBoundProjections.Discover();
        var modelBoundHandlers = modelBoundDefinitions.ToDictionary(
            kvp => kvp.Key,
            kvp => new ProjectionHandler(eventStore, kvp.Value.Identifier, kvp.Key, kvp.Value.ReadModel, kvp.Value.EventSequenceId) as IProjectionHandler);

        _definitionsByType = FindAllProjectionDefinitions(
            eventTypes,
            clientArtifacts,
            serviceProvider,
            jsonSerializerOptions);

        _handlersByType = _definitionsByType.ToDictionary(
                kvp => kvp.Key,
                kvp => new ProjectionHandler(eventStore, kvp.Value.Identifier, kvp.Key.GetReadModelType(), kvp.Value.ReadModel, kvp.Value.EventSequenceId) as IProjectionHandler);

        _handlersByModelType = _handlersByType.ToDictionary(
            _ => _.Key.GetReadModelType(),
            _ => _.Value);
        _handlersByModelType = _handlersByModelType.Concat(modelBoundHandlers).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Definitions =
            ((IEnumerable<ProjectionDefinition>)[
                .. _definitionsByType.Values.Select(_ => _).ToList(),
                .. modelBoundDefinitions.Values
            ]).ToImmutableList();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        await _servicesAccessor.Services.Projections.Register(new()
        {
            EventStore = eventStore.Name,
            Owner = ProjectionOwner.Client,
            Projections = [.. Definitions]
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey)
    {
        var handler = _handlersByModelType[typeof(TReadModel)];
        var request = new GetSnapshotsByIdRequest
        {
            ProjectionId = handler.Id,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId.Log,
            ReadModelKey = readModelKey
        };

        var response = await _servicesAccessor.Services.Projections.GetSnapshotsById(request);
        var snapshots = new List<ProjectionSnapshot<TReadModel>>();

        foreach (var snapshot in response.Snapshots)
        {
            var readModel = JsonSerializer.Deserialize<TReadModel>(snapshot.ReadModel, jsonSerializerOptions)!;
            var events = snapshot.Events.ToClient(jsonSerializerOptions);
            snapshots.Add(new ProjectionSnapshot<TReadModel>(
                readModel,
                events,
                snapshot.Occurred,
                snapshot.CorrelationId));
        }

        return snapshots;
    }

    Dictionary<Type, ProjectionDefinition> FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        JsonSerializerOptions jsonSerializerOptions) =>
        clientArtifacts.Projections
                .ToDictionary(
                    _ => _,
                    _ =>
                    {
                        var modelType = _.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
                        var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
                        var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
                        return (method.Invoke(
                            null,
                            [
                                _,
                                namingPolicy,
                                eventTypes,
                                serviceProvider,
                                jsonSerializerOptions
                            ]) as ProjectionDefinition)!;
                    });

    static class ProjectionDefinitionCreator<TReadModel>
        where TReadModel : class
    {
        public static ProjectionDefinition CreateAndDefine(
            Type type,
            INamingPolicy namingPolicy,
            IEventTypes eventTypes,
            IServiceProvider serviceProvider,
            JsonSerializerOptions jsonSerializerOptions)
        {
            var instance = (serviceProvider.GetRequiredService(type) as IProjectionFor<TReadModel>)!;
            var builder = new ProjectionBuilderFor<TReadModel>(type.GetProjectionId(), type, namingPolicy, eventTypes, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }
}
