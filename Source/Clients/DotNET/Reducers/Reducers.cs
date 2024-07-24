// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Models;
using Cratis.Reflection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reducers"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
/// <param name="clientArtifacts"><see cref="IClientArtifactsProvider"/> for discovery.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
/// <param name="reducerValidator"><see cref="IReducerValidator"/> for validating reducer types.</param>
/// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing of events.</param>
/// <param name="modelNameResolver"><see cref="IModelNameResolver"/> for resolving read model names.</param>
/// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class Reducers(
    IEventStore eventStore,
    IClientArtifactsProvider clientArtifacts,
    IServiceProvider serviceProvider,
    IReducerValidator reducerValidator,
    IEventTypes eventTypes,
    IEventSerializer eventSerializer,
    IModelNameResolver modelNameResolver,
    IJsonSchemaGenerator jsonSchemaGenerator,
    ILogger<Reducers> logger) : IReducers
{
    IEnumerable<Type> _aggregateRootStateTypes = [];
    IDictionary<Type, IReducerHandler> _handlers = new Dictionary<Type, IReducerHandler>();

    /// <inheritdoc/>
    public Task Discover()
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
                                    var eventSequenceId = reducerType.GetEventSequenceId();
                                    return new ReducerHandler(
                                        reducerType.GetReducerId(),
                                        eventSequenceId,
                                        new ReducerInvoker(
                                            serviceProvider,
                                            eventTypes,
                                            reducerType,
                                            readModelType),
                                        eventSerializer,
                                        ShouldReducerBeActive(reducerType, readModelType)) as IReducerHandler;
                                });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        logger.RegisterReducers();

        foreach (var reducerHandler in _handlers.Values)
        {
            logger.RegisterReducer(
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
                Name = modelNameResolver.GetNameFor(_.ReadModelType),
                Schema = jsonSchemaGenerator.Generate(_.ReadModelType).ToJson()
            },
            SinkTypeId = WellKnownSinkTypes.MongoDB
        }).ToArray();

        // await _connection.PerformCommand(route, registrations);
        await Task.CompletedTask;
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

    bool ShouldReducerBeActive(Type reducerType, Type readModelType)
    {
        var active = reducerType.IsActive();
        if (!active || _aggregateRootStateTypes.Contains(readModelType))
        {
            return false;
        }

        return active;
    }

    void ThrowIfTypeIsNotAReducer(Type reducerType)
    {
        if (!_handlers.ContainsKey(reducerType))
        {
            throw new UnknownReducerType(reducerType);
        }
    }
}
