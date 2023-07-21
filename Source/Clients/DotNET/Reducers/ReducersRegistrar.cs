// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Schemas;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducersRegistrar"/>.
/// </summary>
public class ReducersRegistrar : IReducersRegistrar
{
    readonly IDictionary<Type, IReducerHandler> _handlers;
    readonly IExecutionContextManager _executionContextManager;
    readonly IModelNameResolver _modelNameResolver;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IConnection _connection;
    readonly ILogger<ReducersRegistrar> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ReducersRegistrar"/>.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="reducerValidator"><see cref="IReducerValidator"/> for validating reducer types.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="modelNameResolver"><see cref="IModelNameResolver"/> for resolving read model names.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="connection"><see cref="IConnection"/> for working with kernel.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ReducersRegistrar(
        IExecutionContextManager executionContextManager,
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IReducerValidator reducerValidator,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IModelNameResolver modelNameResolver,
        IJsonSchemaGenerator jsonSchemaGenerator,
        IConnection connection,
        ILogger<ReducersRegistrar> logger)
    {
        _handlers = clientArtifacts.Reducers
                            .ToDictionary(
                                _ => _,
                                reducerType =>
                                {
                                    reducerValidator.Validate(reducerType);
                                    var reducer = reducerType.GetCustomAttribute<ReducerAttribute>()!;
                                    return new ReducerHandler(
                                        reducer.ReducerId,
                                        reducerType.FullName ?? $"{reducerType.Namespace}.{reducerType.Name}",
                                        reducer.EventSequenceId,
                                        eventTypes,
                                        new ReducerInvoker(
                                            serviceProvider,
                                            eventTypes,
                                            reducerType,
                                            reducerType.GetReadModelType()),
                                        eventSerializer) as IReducerHandler;
                                });
        _executionContextManager = executionContextManager;
        _modelNameResolver = modelNameResolver;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _connection = connection;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IReducerHandler GetById(ReducerId id) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task Initialize()
    {
        _logger.RegisterReducers();

        foreach (var reducerHandler in _handlers.Values)
        {
            _logger.RegisterReducer(
                reducerHandler.ReducerId,
                reducerHandler.Name,
                reducerHandler.EventSequenceId);
        }

        var microserviceId = _executionContextManager.Current.MicroserviceId;
        var route = $"/api/events/store/{microserviceId}/reducers/register/{_connection.ConnectionId}";

        var registrations = _handlers.Values.Select(_ => new ClientReducerRegistration(
             _.ReducerId,
             _.Name,
             _.EventSequenceId,
             _.EventTypes.Select(et => new EventTypeWithKeyExpression(et, "$eventSourceId")).ToArray(),
             new ModelDefinition(
                 _modelNameResolver.GetNameFor(_.ReadModelType),
                 _jsonSchemaGenerator.Generate(_.ReadModelType).ToJson()),
                 WellKnownProjectionSinkTypes.MongoDB
             )).ToArray();

        await _connection.PerformCommand(route, registrations);
    }
}
