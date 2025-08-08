// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Projections.for_ProjectionWatcher.given;

public class a_watcher : Specification
{
    protected IEventStore _eventStore;
    protected IProjections _projections;
    protected IChronicleConnection _connection;
    protected IConnectionLifecycle _lifecycle;
    internal IChronicleServicesAccessor _serviceAccessor;
    internal IServices _services;
    internal Contracts.Projections.IProjections _projectionsService;
    protected ProjectionWatcher<SomeModel> _watcher;
    protected int _stopCount;

    protected EventStoreName _eventStoreName;
    protected ProjectionId _projectionId;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStoreName = Guid.NewGuid().ToString();
        _eventStore.Name.Returns(_eventStoreName);
        _projections = Substitute.For<IProjections>();
        _projectionId = Guid.NewGuid().ToString();
        _projections.GetProjectionIdForModel<SomeModel>().Returns(_projectionId);

        _eventStore.Projections.Returns(_projections);
        _services = Substitute.For<IServices>();
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _serviceAccessor = _connection as IChronicleServicesAccessor;
        _serviceAccessor.Services.Returns(_services);
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _connection.Lifecycle.Returns(_lifecycle);
        _eventStore.Connection.Returns(_connection);
        _projectionsService = Substitute.For<Contracts.Projections.IProjections>();
        _services.Projections.Returns(_projectionsService);
        _watcher = new ProjectionWatcher<SomeModel>(_eventStore, () => _stopCount++, JsonSerializerOptions.Default);
    }
}
