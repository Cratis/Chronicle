// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Models;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_Reducers.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IClientArtifactsProvider _clientArtifacts;
    protected IServiceProvider _serviceProvider;
    protected IReducerValidator _reducerValidator;
    protected IEventTypes _eventTypes;
    protected IEventSerializer _eventSerializer;
    protected IModelNameResolver _modelNameResolver;
    protected IJsonSchemaGenerator _jsonSchemaGenerator;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected ILogger<Reducers> _logger;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected IObservers _observers;
    protected Dictionary<Type, IReducerHandler> _handlersByType;
    protected Dictionary<Type, IReducerHandler> _handlersByModelType;
    protected Reducers _reducers;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _reducerValidator = Substitute.For<IReducerValidator>();
        _eventTypes = Substitute.For<IEventTypes>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _modelNameResolver = Substitute.For<IModelNameResolver>();
        _jsonSchemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        _jsonSerializerOptions = new();
        _logger = Substitute.For<ILogger<Reducers>>();

        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);
        _servicesAccessor = Substitute.For<IChronicleServicesAccessor>();
        _servicesAccessor.Services.Returns(_services);

        var connection = Substitute.For<IChronicleConnection>();
        _eventStore.Connection.Returns(connection);

        _handlersByType = new Dictionary<Type, IReducerHandler>();
        _handlersByModelType = new Dictionary<Type, IReducerHandler>();

        _clientArtifacts.Reducers.Returns([]);
        _clientArtifacts.AggregateRootStateTypes.Returns([]);

        _reducers = new Reducers(
            _eventStore,
            _clientArtifacts,
            _serviceProvider,
            _reducerValidator,
            _eventTypes,
            _eventSerializer,
            _modelNameResolver,
            _jsonSchemaGenerator,
            _jsonSerializerOptions,
            _logger);

        // Use reflection to set the private handler fields
        var handlersByTypeField = typeof(Reducers).GetField("_handlersByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlersByTypeField?.SetValue(_reducers, _handlersByType);

        var handlersByModelTypeField = typeof(Reducers).GetField("_handlersByModelType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlersByModelTypeField?.SetValue(_reducers, _handlersByModelType);
    }
}
