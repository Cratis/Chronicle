// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatcherManager.when_getting_watcher;

public class second_time_and_client_is_disconnected : given.a_projection_watcher_manager
{
    IProjectionWatcher<SomeModel> _expectedWatcher;
    IProjectionWatcher<SomeModel> _firstWatcher;
    IProjectionWatcher<SomeModel> _secondWatcher;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(false);

        _expectedWatcher = Substitute.For<IProjectionWatcher<SomeModel>>();
        _projectionWatcherFactory.Create<SomeModel>(Arg.Any<Action>()).Returns(_expectedWatcher);

        _firstWatcher = _manager.GetWatcher<SomeModel>();
    }

    void Because() => _secondWatcher = _manager.GetWatcher<SomeModel>();

    [Fact] void should_return_watcher_first_time() => _firstWatcher.ShouldNotBeNull();
    [Fact] void should_return_watcher_second_time() => _secondWatcher.ShouldNotBeNull();
    [Fact] void should_return_expected_watcher_first_time() => _firstWatcher.ShouldEqual(_expectedWatcher);
    [Fact] void should_return_expected_watcher_second_time() => _secondWatcher.ShouldEqual(_expectedWatcher);
    [Fact] void should_not_start_watcher() => _expectedWatcher.DidNotReceive().Start();
}
