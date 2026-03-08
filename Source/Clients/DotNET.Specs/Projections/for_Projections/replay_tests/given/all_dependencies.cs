// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.replay_tests.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IClientArtifactsProvider _clientArtifacts;
    protected INamingPolicy _namingPolicy;
    protected IEventSerializer _eventSerializer;
    protected IClientArtifactsActivator _artifactsActivator;
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
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _namingPolicy = new DefaultNamingPolicy();
        _artifactsActivator = Substitute.For<IClientArtifactsActivator>();
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
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);

        // Use reflection to set the private handler fields
        var handlersByTypeField = typeof(Projections).GetField("_handlersByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlersByTypeField?.SetValue(_projections, _handlersByType);

        var handlersByModelTypeField = typeof(Projections).GetField("_handlersByModelType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        handlersByModelTypeField?.SetValue(_projections, _handlersByModelType);
    }
}
