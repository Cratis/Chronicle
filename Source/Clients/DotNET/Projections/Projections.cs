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
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Registrations;
using Cratis.Monads;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

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
/// <param name="artifactsActivator"><see cref="IClientArtifactsActivator"/> for activating instances of projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
/// <param name="logger"><see cref="ILogger{Projections}"/> for logging.</param>
public class Projections(
    IEventStore eventStore,
    IEventTypes eventTypes,
    IClientArtifactsProvider clientArtifacts,
    INamingPolicy namingPolicy,
    IClientArtifactsActivator artifactsActivator,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<Projections> logger) : IProjections
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
    Dictionary<Type, IProjectionHandler> _handlersByType = new();
    Dictionary<Type, IProjectionHandler> _handlersByModelType = new();
    Dictionary<Type, IProjectionHandler> _modelBoundHandlers = new();
    Dictionary<Type, ProjectionDefinition> _definitionsByType = new();
    bool _discovered;

    /// <summary>
    /// Gets all the <see cref="ProjectionDefinition">projection definitions</see>.
    /// </summary>
    internal IImmutableList<ProjectionDefinition> Definitions { get; private set; } = ImmutableList<ProjectionDefinition>.Empty;

    /// <summary>
    /// Gets the per-artifact outcome of the last <see cref="Discover"/>, covering every declared projection artifact -
    /// the fluent <see cref="IProjectionFor{TReadModel}"/> implementations and the model-bound read models.
    /// </summary>
    /// <remarks>
    /// An artifact with no failure has a definition and travels in the batch <see cref="Register"/> sends. This is a
    /// discovery fact rather than a verdict: it is <see cref="IEventStore.RegisterAll"/> that publishes it as an
    /// outcome, and only once the kernel call carrying these definitions has returned.
    /// </remarks>
    internal IImmutableList<ArtifactRegistration> ArtifactRegistrations { get; private set; } = ImmutableList<ArtifactRegistration>.Empty;

    /// <inheritdoc/>
    public bool HasFor(ProjectionId projectionId) => Definitions.Any(_ => _.Identifier == projectionId);

    /// <inheritdoc/>
    public bool HasFor<TReadModel>() => _handlersByModelType.ContainsKey(typeof(TReadModel));

    /// <inheritdoc/>
    public bool HasFor(Type readModelType) => _handlersByModelType.ContainsKey(readModelType);

    /// <inheritdoc/>
    public IEnumerable<IProjectionHandler> GetAllHandlers() => _handlersByType.Values.Concat(_modelBoundHandlers.Values);

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
        var handler = GetHandlerForProjectionOrReadModelType(projectionType);
        return handler.GetFailedPartitions();
    }

    /// <inheritdoc/>
    public Task<ProjectionState> GetStateFor<TProjection>()
        where TProjection : IProjection
    {
        var projectionType = typeof(TProjection);
        var handler = _handlersByType[projectionType];
        return handler.GetState();
    }

    /// <inheritdoc/>
    public Task<ProjectionState> GetStateForModel<TReadModel>() => GetStateForModel(typeof(TReadModel));

    /// <inheritdoc/>
    public Task<ProjectionState> GetStateForModel(Type readModelType) => _handlersByModelType[readModelType].GetState();

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsForModel<TReadModel>() => GetFailedPartitionsForModel(typeof(TReadModel));

    /// <inheritdoc/>
    public Task<IEnumerable<Observation.FailedPartition>> GetFailedPartitionsForModel(Type readModelType) => _handlersByModelType[readModelType].GetFailedPartitions();

    /// <inheritdoc/>
    public Task<JobId> Replay<TProjection>()
        where TProjection : IProjection
    {
        var projectionType = typeof(TProjection);
        var handler = _handlersByType[projectionType];
        return Replay(handler.Id);
    }

    /// <inheritdoc/>
    public async Task<JobId> Replay(ProjectionId projectionId)
    {
        var response = await _servicesAccessor.Services.Observers.Replay(new Contracts.Observation.Replay
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ObserverId = projectionId,
            EventSequenceId = string.Empty
        });
        return Guid.TryParse(response.JobId, out var value) ? new JobId(value) : JobId.NotSet;
    }

    /// <inheritdoc/>
    public IEnumerable<(string EventStoreName, IEnumerable<EventTypeId> EventTypeIds)> GetExternalEventStoreSubscriptions()
    {
        return Definitions
            .Where(d => d.EventSequenceId.StartsWith(EventSequenceId.InboxPrefix, StringComparison.Ordinal))
            .GroupBy(d => d.EventSequenceId[EventSequenceId.InboxPrefix.Length..])
            .Select(g => (
                EventStoreName: g.Key,
                EventTypeIds: g.SelectMany(d => d.From.Keys
                    .Concat(d.Join.Keys)
                    .Select(et => new EventTypeId(et.Id)))
                    .Distinct()
                    .AsEnumerable()))
            .ToList();
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        var modelBoundProjections = new ModelBoundProjections(clientArtifacts, namingPolicy, eventTypes, logger, eventStore.Name?.Value);
        var modelBoundDefinitions = modelBoundProjections.Discover();
        var modelBoundHandlers = modelBoundDefinitions.ToDictionary(
            kvp => kvp.Key,
            kvp => new ProjectionHandler(eventStore, kvp.Value.Identifier, kvp.Key, kvp.Value.ReadModel, kvp.Value.EventSequenceId) as IProjectionHandler);

        var (definitionsByType, failures) = FindAllProjectionDefinitions(
            eventTypes,
            clientArtifacts,
            artifactsActivator,
            jsonSerializerOptions);
        _definitionsByType = definitionsByType;

        _handlersByType = _definitionsByType.ToDictionary(
                kvp => kvp.Key,
                kvp => new ProjectionHandler(eventStore, kvp.Value.Identifier, kvp.Key.GetReadModelType(), kvp.Value.ReadModel, kvp.Value.EventSequenceId) as IProjectionHandler);

        _modelBoundHandlers = modelBoundHandlers;

        // The read model index holds one handler per read model, and asking for a projection by the model it maintains
        // is the only handle a model-bound projection has. So a read model claimed twice leaves the second projection
        // addressable only if it has a type of its own - and a model-bound one does not. Both are still registered and
        // both still write to the read model, which is the part worth knowing about; say so rather than resolving it
        // silently by declaration order.
        _handlersByModelType = new Dictionary<Type, IProjectionHandler>();
        var claimedBy = new Dictionary<Type, string>();

        foreach (var kvp in _handlersByType)
        {
            var readModelType = kvp.Key.GetReadModelType();
            if (_handlersByModelType.TryAdd(readModelType, kvp.Value))
            {
                claimedBy[readModelType] = kvp.Key.FullName ?? kvp.Key.Name;
            }
            else
            {
                logger.MoreThanOneProjectionForReadModel(readModelType, kvp.Key.FullName ?? kvp.Key.Name, claimedBy[readModelType]);
            }
        }

        foreach (var kvp in _modelBoundHandlers)
        {
            if (!_handlersByModelType.TryAdd(kvp.Key, kvp.Value))
            {
                logger.MoreThanOneProjectionForReadModel(kvp.Key, $"the model-bound projection on {kvp.Key.Name}", claimedBy[kvp.Key]);
            }
        }

        Definitions =
            ((IEnumerable<ProjectionDefinition>)[
                .. _definitionsByType.Values.Select(_ => _).ToList(),
                .. modelBoundDefinitions.Values
            ]).ToImmutableList();

        ArtifactRegistrations =
            ((IEnumerable<ArtifactRegistration>)[
                .. _definitionsByType.Keys.Select(type => new ArtifactRegistration(type, null)),
                .. modelBoundDefinitions.Keys.Select(type => new ArtifactRegistration(type, null)),
                .. failures.Select(kvp => new ArtifactRegistration(kvp.Key, kvp.Value)),
                .. modelBoundProjections.Failures.Select(kvp => new ArtifactRegistration(kvp.Key, kvp.Value))
            ]).ToImmutableList();

        _discovered = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        await _servicesAccessor.Services.Projections.Register(new()
        {
            EventStore = eventStore.Name,
            Owner = ProjectionOwner.Client,
            Projections = [.. Definitions],

            // The registration only claims to be the client's full set when discovery has run and every discovered
            // artifact produced a definition. An artifact whose definition could not be built is excluded from the
            // batch, and claiming a full set then would make the kernel retire a projection that still exists in
            // the client.
            FullSet = _discovered && ArtifactRegistrations.All(registration => registration.IsRegistered)
        });
    }

    /// <inheritdoc/>
    public async Task<ProjectionQueryResult> Query(string declaration, string eventSequenceId = "event-log")
    {
        var result = await _servicesAccessor.Services.Projections.Preview(new PreviewProjectionRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = eventSequenceId,
            Declaration = declaration
        });

        if (result.Value1 is not null)
        {
            throw new UnableToQueryProjection(result.Value1.Errors.Select(e => e.Message));
        }

        var queryResult = result.Value0!;
        return new ProjectionQueryResult([.. queryResult.ReadModelEntries]);
    }

    /// <summary>
    /// Resolve the <see cref="IProjectionHandler"/> for a type that is either a projection type or a read model type.
    /// </summary>
    /// <param name="type">The projection type or read model type to resolve for.</param>
    /// <returns>The <see cref="IProjectionHandler"/> for the type.</returns>
    /// <remarks>
    /// Fluent projections are addressable by their own type, while model-bound projections have no projection type at
    /// all - their handler is only ever keyed by the read model it projects to. A caller holding either handle has to
    /// land on the same handler, so the projection type is tried first and the read model type second.
    /// </remarks>
    IProjectionHandler GetHandlerForProjectionOrReadModelType(Type type) =>
        _handlersByType.TryGetValue(type, out var handler) ? handler : _handlersByModelType[type];

    /// <summary>
    /// Builds a definition for every declared fluent projection, isolating the ones that cannot be built.
    /// </summary>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="clientArtifacts">The <see cref="IClientArtifactsProvider"/> holding the declared projections.</param>
    /// <param name="artifactsActivator"><see cref="IClientArtifactsActivator"/> for activating instances of projections.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    /// <returns>The definitions that could be built, and the failure that stopped each one that could not.</returns>
    /// <remarks>
    /// A projection that cannot be built is logged and skipped so that it costs itself and nothing else. The failure is
    /// returned alongside rather than only logged, so that its read model can be told apart from one never declared.
    /// </remarks>
    (Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures) FindAllProjectionDefinitions(
        IEventTypes eventTypes,
        IClientArtifactsProvider clientArtifacts,
        IClientArtifactsActivator artifactsActivator,
        JsonSerializerOptions jsonSerializerOptions)
    {
        var result = new Dictionary<Type, ProjectionDefinition>();
        var failures = new Dictionary<Type, Exception>();
        foreach (var projectionType in clientArtifacts.Projections)
        {
            var modelType = projectionType.GetInterface(typeof(IProjectionFor<>).Name)!.GetGenericArguments()[0]!;
            var creatorType = typeof(ProjectionDefinitionCreator<>).MakeGenericType(modelType);
            var method = creatorType.GetMethod(nameof(ProjectionDefinitionCreator<object>.CreateAndDefine), BindingFlags.Public | BindingFlags.Static)!;
            var createProjectionDefinitionResult = (method.Invoke(
                null,
                [
                    projectionType,
                    namingPolicy,
                    eventTypes,
                    artifactsActivator,
                    jsonSerializerOptions
                ]) as Catch<ProjectionDefinition>)!;
            if (createProjectionDefinitionResult.TryGetException(out var exception))
            {
                logger.FailedToCreateProjectionDefinition(projectionType, exception);
                failures[projectionType] = exception;
                continue;
            }
            result.Add(projectionType, createProjectionDefinitionResult.AsT0);
        }

        return (result, failures);
    }

    static class ProjectionDefinitionCreator<TReadModel>
        where TReadModel : class
    {
        public static Catch<ProjectionDefinition> CreateAndDefine(
            Type type,
            INamingPolicy namingPolicy,
            IEventTypes eventTypes,
            IClientArtifactsActivator artifactsActivator,
            JsonSerializerOptions jsonSerializerOptions)
        {
            try
            {
                var activateArtifactResult = artifactsActivator.ActivateNonDisposable<IProjectionFor<TReadModel>>(type);
                if (activateArtifactResult.TryGetException(out var exception))
                {
                    return exception;
                }

                var builder = new ProjectionBuilderFor<TReadModel>(type.GetProjectionId(), type, namingPolicy, eventTypes, jsonSerializerOptions);
                activateArtifactResult.AsT0.Define(builder);
                return builder.Build();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
