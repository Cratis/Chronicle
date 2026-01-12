// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcherManager.given;

public class a_projection_watcher_manager : Specification
{
    protected IReadModelWatcherFactory _projectionWatcherFactory;
    protected IEventStore _eventStore;
    protected IChronicleConnection _connection;
    protected IConnectionLifecycle _lifecycle;
    protected ReadModelWatcherManager _manager;

    void Establish()
    {
        _projectionWatcherFactory = Substitute.For<IReadModelWatcherFactory>();
        _eventStore = Substitute.For<IEventStore>();
        _connection = Substitute.For<IChronicleConnection>();
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _eventStore.Connection.Returns(_connection);
        _connection.Lifecycle.Returns(_lifecycle);
        _manager = new ReadModelWatcherManager(_projectionWatcherFactory, _eventStore);
    }
}
