// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected INamingPolicy _namingPolicy;
    protected IProjections _projections;
    protected IReducers _reducers;
    protected IEventTypes _eventTypes;
    protected IEnumerable<IHaveReadModel> _additionalReadModels;
    protected IJsonSchemaGenerator _schemaGenerator;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected IReadModelWatcherManager _readModelWatcherManager;
    protected IReducerObservers _reducerObservers;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected ReadModels _readModels;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _namingPolicy = new DefaultNamingPolicy();
        _projections = Substitute.For<IProjections>();
        _reducers = Substitute.For<IReducers>();
        _eventTypes = Substitute.For<IEventTypes>();
        _additionalReadModels = [];
        _schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        _readModelWatcherManager = Substitute.For<IReadModelWatcherManager>();
        _reducerObservers = Substitute.For<IReducerObservers>();
        _jsonSerializerOptions = new();

        _services = Substitute.For<IServices>();

        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = connection as IChronicleServicesAccessor;
        _servicesAccessor.Services.Returns(_services);
        _eventStore.Connection.Returns(connection);

        _readModels = new ReadModels(
            _eventStore,
            _namingPolicy,
            _projections,
            _reducers,
            _eventTypes,
            _schemaGenerator,
            _jsonSerializerOptions,
            _readModelWatcherManager,
            _reducerObservers);
    }
}
