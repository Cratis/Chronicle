// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher.given;

public class a_watcher : Specification
{
    protected IEventStore _eventStore;
    protected IChronicleConnection _connection;
    protected IConnectionLifecycle _lifecycle;
    internal IChronicleServicesAccessor _serviceAccessor;
    internal IServices _services;
    internal Contracts.ReadModels.IReadModels _readModelsService;
    protected ReadModelWatcher<SomeModel> _watcher;
    protected int _stopCount;
    protected EventStoreName _eventStoreName;
    protected EventStoreNamespaceName _eventStoreNamespace = EventStoreNamespaceName.Default;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStoreName = Guid.NewGuid().ToString();
        _eventStore.Name.Returns(_eventStoreName);
        _eventStore.Namespace.Returns(_eventStoreNamespace);

        _services = Substitute.For<IServices>();
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _serviceAccessor = _connection as IChronicleServicesAccessor;
        _serviceAccessor.Services.Returns(_services);
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _connection.Lifecycle.Returns(_lifecycle);
        _eventStore.Connection.Returns(_connection);
        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
        _watcher = new ReadModelWatcher<SomeModel>(_eventStore, () => _stopCount++, JsonSerializerOptions.Default);
    }
}
