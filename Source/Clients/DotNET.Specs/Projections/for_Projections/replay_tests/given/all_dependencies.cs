// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Schemas;
using Cratis.Models;

namespace Cratis.Chronicle.Projections.for_Projections.replay_tests.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IProjectionWatcherManager _projectionWatcherManager;
    protected IClientArtifactsProvider _clientArtifacts;
    internal IRulesProjections _rulesProjections;
    protected IJsonSchemaGenerator _schemaGenerator;
    protected IModelNameResolver _modelNameResolver;
    protected IEventSerializer _eventSerializer;
    protected IServiceProvider _serviceProvider;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected IObservers _observers;
    protected Dictionary<Type, IProjectionHandler> _handlersByType;
    protected Dictionary<Type, IProjectionHandler> _handlersByModelType;
    protected Projections _projections;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _eventTypes = Substitute.For<IEventTypes>();
        _projectionWatcherManager = Substitute.For<IProjectionWatcherManager>();
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _rulesProjections = Substitute.For<IRulesProjections>();
        _schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        _modelNameResolver = Substitute.For<IModelNameResolver>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _jsonSerializerOptions = new();

        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = connection as IChronicleServicesAccessor;
        _servicesAccessor.Services.Returns(_services);
        _eventStore.Connection.Returns(connection);

        _handlersByType = new Dictionary<Type, IProjectionHandler>();
        _handlersByModelType = new Dictionary<Type, IProjectionHandler>();

        _clientArtifacts.Projections.Returns([]);

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _projectionWatcherManager,
            _clientArtifacts,
            _schemaGenerator,
            _modelNameResolver,
            _eventSerializer,
            _serviceProvider,
            _jsonSerializerOptions);

        // Use reflection to set the private handler fields
        var handlersByTypeField = typeof(Projections).GetField("_handlersByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlersByTypeField?.SetValue(_projections, _handlersByType);

        var handlersByModelTypeField = typeof(Projections).GetField("_handlersByModelType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlersByModelTypeField?.SetValue(_projections, _handlersByModelType);
    }
}
