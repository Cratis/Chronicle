// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IClientArtifactsProvider _clientArtifactsProvider;
    protected IServiceProvider _serviceProvider;
    protected IClientArtifactsActivator _artifactActivator;
    protected IActivateReactorMiddlewares _middlewaresActivator;
    protected IReactorMiddlewares _middlewares;
    protected IEventSerializer _eventSerializer;
    protected ICausationManager _causationManager;
    protected IActivitySource<Reactors> _activitySource;
    System.Diagnostics.ActivitySource _traceSource;
    protected IReactorSideEffectHandlers _sideEffectHandlers;
    protected IReactorContextValuesBuilder _reactorContextValuesBuilder;
    protected ILogger<Reactors> _logger;
    protected ILoggerFactory _loggerFactory;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected IObservers _observers;
    protected IConnectionLifecycle _connectionLifecycle;
    protected IIdentityProvider _identityProvider;
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
        _middlewaresActivator = Substitute.For<IActivateReactorMiddlewares>();
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _middlewaresActivator.Activate(Arg.Any<IServiceProvider>()).Returns(_middlewares);
        _eventSerializer = Substitute.For<IEventSerializer>();
        _causationManager = CreateCausationManager();
        _traceSource = new System.Diagnostics.ActivitySource("reactor-specification");
        _activitySource = new Cratis.Traces.ActivitySource<Reactors>(_traceSource);
        _sideEffectHandlers = Substitute.For<IReactorSideEffectHandlers>();
        _reactorContextValuesBuilder = Substitute.For<IReactorContextValuesBuilder>();
        _logger = CreateLogger();
        _loggerFactory = Substitute.For<ILoggerFactory>();

        _connectionLifecycle = Substitute.For<IConnectionLifecycle>();
        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"test-connection-id");
        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);
        var contractReactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(contractReactors);
        contractReactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Never<EventsToObserve>());

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = connection as IChronicleServicesAccessor;
        _servicesAccessor.Services.Returns(_services);
        _eventStore.Connection.Returns(connection);
        connection.Lifecycle.Returns(_connectionLifecycle);
        _eventStore.Connection.Returns(connection);

        _identityProvider = Substitute.For<IIdentityProvider>();

        _clientArtifactsProvider.Reactors.Returns([]);

        _reactors = new Reactors(
            _eventStore,
            _eventTypes,
            _clientArtifactsProvider,
            _serviceProvider,
            _artifactActivator,
            _middlewaresActivator,
            _eventSerializer,
            _causationManager,
            _identityProvider,
            _activitySource,
            _sideEffectHandlers,
            _reactorContextValuesBuilder,
            new ReactorMethodArgumentsResolver(),
            _logger,
            _loggerFactory);
    }

    protected virtual ICausationManager CreateCausationManager() => Substitute.For<ICausationManager>();

    protected virtual ILogger<Reactors> CreateLogger() => Substitute.For<ILogger<Reactors>>();

    void Destroy() => _traceSource.Dispose();
}
