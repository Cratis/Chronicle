// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Projections.for_ProjectionWatcherManager.given;

public class a_projection_watcher_manager : Specification
{
    protected IProjectionWatcherFactory _projectionWatcherFactory;
    protected IEventStore _eventStore;
    protected IChronicleConnection _connection;
    protected IConnectionLifecycle _lifecycle;
    protected ProjectionWatcherManager _manager;

    void Establish()
    {
        _projectionWatcherFactory = Substitute.For<IProjectionWatcherFactory>();
        _eventStore = Substitute.For<IEventStore>();
        _connection = Substitute.For<IChronicleConnection>();
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _eventStore.Connection.Returns(_connection);
        _connection.Lifecycle.Returns(_lifecycle);
        _manager = new ProjectionWatcherManager(_projectionWatcherFactory, _eventStore);
    }
}
