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
    static class ImmediateProjectionsCache<TProjection>
    {
        public static TProjection? Instance;
        public static ProjectionDefinition? Definition;
    }

    readonly IModelNameConvention _modelNameConvention;
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly IConnection _connection;
    readonly List<ProjectionDefinition> _definitions = new();

    /// <inheritdoc/>
    public IImmutableList<ProjectionDefinition> Definitions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjections"/> class.
    /// </summary>
    /// <param name="modelNameConvention">The <see cref="IModelNameConvention"/> to use for naming the models.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for providing projections.</param>
    /// <param name="eventTypes">All the <see cref="IEventTypes"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating model schema.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="projectionSerializer">The <see cref="IJsonProjectionSerializer"/> for serializing projection definitions.</param>
    /// <param name="connection">The <see cref="IConnection"/> for connecting to the kernel.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with the execution context.</param>
    public ImmediateProjections(
        IModelNameConvention modelNameConvention,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions,
        IJsonProjectionSerializer projectionSerializer,
        IConnection connection,
        IExecutionContextManager executionContextManager)
    {
        _modelNameConvention = modelNameConvention;
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
    public Task<ImmediateProjectionResult> GetInstanceById(Type modelType, ModelKey modelKey)
    {
        var projectionDefinition = (typeof(ImmediateProjectionsCache<>).MakeGenericType(modelType)
            .GetField(nameof(ImmediateProjectionsCache<object>.Definition))!
            .GetValue(null) as ProjectionDefinition)!;
        return GetInstanceById(projectionDefinition.Identifier, modelKey);
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult<TModel>> GetInstanceById<TModel>(ModelKey modelKey)
    {
        var result = await GetInstanceById(
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance!.Identifier,
            modelKey);

        var model = result.Model.Deserialize<TModel>(_jsonSerializerOptions)!;
        return new(model, result.AffectedProperties, result.ProjectedEventsCount);
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetInstanceById(ProjectionId identifier, ModelKey modelKey)
    {
        var immediateProjection = new ImmediateProjection(
            identifier,
            EventSequenceId.Log,
            modelKey);

        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/projections/immediate/{_executionContextManager.Current.TenantId}";

        var result = await _connection.PerformCommand(route, immediateProjection);
        var element = (JsonElement)result.Response!;
        return element.Deserialize<ImmediateProjectionResult>(_jsonSerializerOptions)!;
    }

    void HandleProjectionTypeCache<TModel>()
    {
        if (ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance is null)
        {
            var projectionType = _clientArtifacts.ImmediateProjections.Single(_ => _.HasInterface<IImmediateProjectionFor<TModel>>())
                ?? throw new MissingImmediateProjectionForModel(typeof(TModel));

            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance = (_serviceProvider.GetService(projectionType) as IImmediateProjectionFor<TModel>)!;
            var builder = new ProjectionBuilderFor<TModel>(
                ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance.Identifier,
                _modelNameConvention,
                _eventTypes,
                _schemaGenerator,
                _jsonSerializerOptions);

            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Instance.Define(builder);

            var projectionDefinition = builder.Build() with { IsActive = false };
            _definitions.Add(projectionDefinition);
            ImmediateProjectionsCache<IImmediateProjectionFor<TModel>>.Definition = projectionDefinition;
        }
    }
}
