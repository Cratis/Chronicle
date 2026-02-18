// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
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
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class Projections(
    IEventStore eventStore,
    IEventTypes eventTypes,
    IClientArtifactsProvider clientArtifacts,
    INamingPolicy namingPolicy,
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
    public bool HasFor<TReadModel>() => _handlersByModelType.ContainsKey(typeof(TReadModel));

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
            var instance = (ActivatorUtilities.CreateInstance(serviceProvider, type) as IProjectionFor<TReadModel>)!;
            var builder = new ProjectionBuilderFor<TReadModel>(type.GetProjectionId(), type, namingPolicy, eventTypes, jsonSerializerOptions);
            instance.Define(builder);
            return builder.Build();
        }
    }
}
