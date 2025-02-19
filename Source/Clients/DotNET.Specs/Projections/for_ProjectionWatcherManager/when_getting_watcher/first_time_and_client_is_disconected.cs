// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Projections.for_ProjectionWatcherManager.when_getting_watcher;


public record SomeModel();
public record SomeOtherModel();

public class first_time_and_client_is_disconnected : Specification
{
    IProjectionWatcher<SomeModel> _expectedWatcher;
    IProjectionWatcher<SomeModel> _watcher;
    IProjectionWatcherFactory _projectionWatcherFactory;
    IEventStore _eventStore;
    IChronicleConnection _connection;
    IConnectionLifecycle _lifecycle;

    ProjectionWatcherManager _manager;


    void Establish()
    {
        _projectionWatcherFactory = Substitute.For<IProjectionWatcherFactory>();
        _eventStore = Substitute.For<IEventStore>();
        _connection = Substitute.For<IChronicleConnection>();
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _eventStore.Connection.Returns(_connection);
        _connection.Lifecycle.Returns(_lifecycle);
        _lifecycle.IsConnected.Returns(false);

        _manager = new ProjectionWatcherManager(_projectionWatcherFactory, _eventStore);

        _expectedWatcher = Substitute.For<IProjectionWatcher<SomeModel>>();
        _projectionWatcherFactory.Create<SomeModel>(Arg.Any<Action>()).Returns(_expectedWatcher);
    }

    void Because() => _watcher = _manager.GetWatcher<SomeModel>();

    [Fact] void should_return_watcher() => _watcher.ShouldNotBeNull();
    [Fact] void should_return_expected_watcher() => _watcher.ShouldEqual(_expectedWatcher);
    [Fact] void should_not_start_watcher() => _expectedWatcher.DidNotReceive().Start();
}
