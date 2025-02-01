// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Models;
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
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
/// <param name="modelNameResolver">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of projections.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class Projections(
    IEventStore eventStore,
    IEventTypes eventTypes,
    IClientArtifactsProvider clientArtifacts,
    IJsonSchemaGenerator schemaGenerator,
    IModelNameResolver modelNameResolver,
    IEventSerializer eventSerializer,
    IServiceProvider serviceProvider,
    JsonSerializerOptions jsonSerializerOptions) : IProjections
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
    IRulesProjections? _rulesProjections;

    IDictionary<Type, ProjectionDefinition> _definitionsByModelType = new Dictionary<Type, ProjectionDefinition>();

    /// <summary>
    /// Gets all the <see cref="ProjectionDefinition">projection definitions</see>.
    /// </summary>
    internal IImmutableList<ProjectionDefinition> Definitions { get; private set; } = ImmutableList<ProjectionDefinition>.Empty;

    /// <inheritdoc/>
    public bool HasFor(ProjectionId projectionId) => Definitions.Any(_ => _.Identifier == projectionId);

    /// <inheritdoc/>
    public bool HasFor(Type modelType) => _definitionsByModelType.ContainsKey(modelType);

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

        var result = await _servicesAccessor.Services.Projections.GetInstanceById(request);
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

        var result = await _servicesAccessor.Services.Projections.GetInstanceById(request);
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

        var result = await _servicesAccessor.Services.Projections.GetInstanceByIdForSession(request);
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

        var result = await _servicesAccessor.Services.Projections.GetInstanceByIdFOrSessionWithEventsApplied(request);
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

        await _servicesAccessor.Services.Projections.DehydrateSession(request);
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
                .. _rulesProjections?.Discover() ?? ImmutableList<ProjectionDefinition>.Empty,
                .. _definitionsByModelType.Values.ToList()
            ]).ToImmutableList();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        await _servicesAccessor.Services.Projections.Register(new()
        {
            EventStore = eventStore.Name,
            Projections = [.. Definitions]
        });
    }

    /// <summary>
    /// Sets the <see cref="IRulesProjections"/>.
    /// </summary>
    /// <param name="rulesProjections"><see cref="IRulesProjections"/> instance to set.</param>
    internal void SetRulesProjections(IRulesProjections rulesProjections) => _rulesProjections = rulesProjections;

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
