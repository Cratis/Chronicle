// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering.given;

public class all_dependencies_with_configured_sink : Specification
{
    protected IEventStore _eventStore;
    protected IClientArtifactsProvider _clientArtifacts;
    protected IServiceProvider _serviceProvider;
    protected IClientArtifactsActivator _artifactActivator;
    protected IReducerValidator _reducerValidator;
    protected IEventTypes _eventTypes;
    protected INamingPolicy _namingPolicy;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected ILogger<Reducers> _logger;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected IObservers _observers;
    protected IIdentityProvider _identityProvider;
    protected IReducerObservers _reducerObservers;
    protected Contracts.Observation.Reducers.IReducers _reducersService;
    protected Reducers _reducers;

    protected virtual SinkTypeId DefaultSinkTypeId => WellKnownSinkTypes.MongoDB;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        var lifecycle = Substitute.For<IConnectionLifecycle>();
        lifecycle.ConnectionId.Returns(ConnectionId.NotSet);
        _eventStore.Connection.Lifecycle.Returns(lifecycle);

        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _artifactActivator = Substitute.For<IClientArtifactsActivator>();
        _reducerValidator = Substitute.For<IReducerValidator>();
        _eventTypes = Substitute.For<IEventTypes>();
        _namingPolicy = new DefaultNamingPolicy();
        _jsonSerializerOptions = new();
        _logger = Substitute.For<ILogger<Reducers>>();

        _reducersService = Substitute.For<Contracts.Observation.Reducers.IReducers>();
        _reducersService.Observe(Arg.Any<IObservable<ReducerMessage>>())
            .Returns(Observable.Empty<ReduceOperationMessage>());

        _observers = Substitute.For<IObservers>();
        _services = Substitute.For<IServices>();
        _services.Observers.Returns(_observers);
        _services.Reducers.Returns(_reducersService);

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = connection as IChronicleServicesAccessor;
        _servicesAccessor.Services.Returns(_services);
        _eventStore.Connection.Returns(connection);
        _eventStore.Connection.Lifecycle.Returns(lifecycle);

        _identityProvider = Substitute.For<IIdentityProvider>();
        _reducerObservers = Substitute.For<IReducerObservers>();

        _reducers = new Reducers(
            _eventStore,
            _clientArtifacts,
            _serviceProvider,
            _artifactActivator,
            _reducerValidator,
            _eventTypes,
            _namingPolicy,
            _jsonSerializerOptions,
            Options.Create(new ChronicleOptions { DefaultSinkTypeId = DefaultSinkTypeId }),
            _identityProvider,
            _reducerObservers,
            _logger);
    }
}
