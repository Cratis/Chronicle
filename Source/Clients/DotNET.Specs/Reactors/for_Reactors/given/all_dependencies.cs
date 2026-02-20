// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_Reactors.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IClientArtifactsProvider _clientArtifactsProvider;
    protected IServiceProvider _serviceProvider;
    protected IClientArtifactsActivator _artifactActivator;
    protected IReactorMiddlewares _middlewares;
    protected IEventSerializer _eventSerializer;
    protected ICausationManager _causationManager;
    protected ILogger<Reactors> _logger;
    protected ILoggerFactory _loggerFactory;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected IObservers _observers;
    protected IConnectionLifecycle _connectionLifecycle;
    protected IIdentityProvider _identityProvider;
    protected Dictionary<Type, IReactorHandler> _handlers;
    protected Reactors _reactors;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _eventTypes = Substitute.For<IEventTypes>();
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _artifactActivator = Substitute.For<IClientArtifactsActivator>();
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _causationManager = Substitute.For<ICausationManager>();
        _logger = Substitute.For<ILogger<Reactors>>();
        _loggerFactory = Substitute.For<ILoggerFactory>();

        _connectionLifecycle = Substitute.For<IConnectionLifecycle>();
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = connection as IChronicleServicesAccessor;
        _servicesAccessor.Services.Returns(_services);
        _eventStore.Connection.Returns(connection);
        connection.Lifecycle.Returns(_connectionLifecycle);
        _eventStore.Connection.Returns(connection);

        _identityProvider = Substitute.For<IIdentityProvider>();

        _handlers = new();

        _clientArtifactsProvider.Reactors.Returns([]);

        _reactors = new Reactors(
            _eventStore,
            _eventTypes,
            _clientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            _middlewares,
            _eventSerializer,
            _causationManager,
            _identityProvider,
            _logger,
            _loggerFactory);

        // Use reflection to set the private _handlers field
        var handlersField = typeof(Reactors).GetField("_handlers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlersField?.SetValue(_reactors, _handlers);
    }
}
