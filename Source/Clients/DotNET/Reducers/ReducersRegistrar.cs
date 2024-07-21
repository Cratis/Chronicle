// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Models;
using Cratis.Reflection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducersRegistrar"/>.
/// </summary>
public class ReducersRegistrar : IReducersRegistrar
{
    readonly IEnumerable<Type> _aggregateRootStateTypes;
    readonly IDictionary<Type, IReducerHandler> _handlers;
    readonly IModelNameResolver _modelNameResolver;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly ILogger<ReducersRegistrar> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ReducersRegistrar"/>.
    /// </summary>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
    /// <param name="reducerValidator"><see cref="IReducerValidator"/> for validating reducer types.</param>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
    /// <param name="modelNameResolver"><see cref="IModelNameResolver"/> for resolving read model names.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ReducersRegistrar(
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IReducerValidator reducerValidator,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IModelNameResolver modelNameResolver,
        IJsonSchemaGenerator jsonSchemaGenerator,
        ILogger<ReducersRegistrar> logger)
    {
        _aggregateRootStateTypes = clientArtifacts
                                            .AggregateRoots
                                            .SelectMany(_ => _.AllBaseAndImplementingTypes())
                                            .Where(_ => _.IsDerivedFromOpenGeneric(typeof(AggregateRoot<>)))
                                            .Select(_ => _.GetGenericArguments()[0])
                                            .ToArray();

        _handlers = clientArtifacts.Reducers
                            .ToDictionary(
                                _ => _,
                                reducerType =>
                                {
                                    var readModelType = reducerType.GetReadModelType();
                                    reducerValidator.Validate(reducerType);
                                    var reducer = reducerType.GetCustomAttribute<ReducerAttribute>()!;
                                    return new ReducerHandler(
                                        reducerType.GetReducerId(),
                                        reducer.EventSequenceId,
                                        new ReducerInvoker(
                                            serviceProvider,
                                            eventTypes,
                                            reducerType,
                                            readModelType),
                                        eventSerializer,
                                        ShouldReducerBeActive(readModelType, reducer)) as IReducerHandler;
                                });
        _modelNameResolver = modelNameResolver;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IEnumerable<IReducerHandler> GetAll() => _handlers.Values;

    /// <inheritdoc/>
    public IReducerHandler GetById(ReducerId reducerId)
    {
        var reducer = _handlers.Values.SingleOrDefault(_ => _.Id == reducerId);
        ReducerDoesNotExist.ThrowIfDoesNotExist(reducerId, reducer);
        return reducer!;
    }

    /// <inheritdoc/>
    public IReducerHandler GetByType(Type reducerType)
    {
        ThrowIfTypeIsNotAReducer(reducerType);
        return _handlers[reducerType];
    }

    /// <inheritdoc/>
    public Type GetClrType(ReducerId reducerId)
    {
        var reducer = _handlers.SingleOrDefault(_ => _.Value.Id == reducerId);
        ReducerDoesNotExist.ThrowIfDoesNotExist(reducerId, reducer.Value);
        return reducer.Key;
    }

    /// <inheritdoc/>
    public IReducerHandler GetForModelType(Type modelType) => _handlers[modelType];

    /// <inheritdoc/>
    public bool HasReducerFor(Type modelType) => _handlers.ContainsKey(modelType);

    /// <inheritdoc/>
    public async Task Initialize()
    {
        _logger.RegisterReducers();

        foreach (var reducerHandler in _handlers.Values)
        {
            _logger.RegisterReducer(
                reducerHandler.Id,
                reducerHandler.EventSequenceId);
        }

        // var route = $"/api/events/store/{eventStore}/reducers/register/{_connection.ConnectionId}";
        var registrations = _handlers.Values.Select(_ => new ReducerDefinition()
        {
            ReducerId = _.Id,
            EventSequenceId = _.EventSequenceId,
            EventTypes = _.EventTypes.Select(et => new EventTypeWithKeyExpression { EventType = et.ToContract(), Key = "$eventSourceId" }).ToArray(),
            ReadModel = new()
            {
                Name = _modelNameResolver.GetNameFor(_.ReadModelType),
                Schema = _jsonSchemaGenerator.Generate(_.ReadModelType).ToJson()
            },
            SinkTypeId = WellKnownSinkTypes.MongoDB
        }).ToArray();

        // await _connection.PerformCommand(route, registrations);
        await Task.CompletedTask;
    }

    bool ShouldReducerBeActive(Type readModelType, ReducerAttribute reducerAttribute)
    {
        if (!reducerAttribute.IsActive || _aggregateRootStateTypes.Contains(readModelType))
        {
            return false;
        }

        return reducerAttribute.IsActive;
    }

    void ThrowIfTypeIsNotAReducer(Type reducerType)
    {
        if (!_handlers.ContainsKey(reducerType))
        {
            throw new UnknownReducerType(reducerType);
        }
    }
}
