// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcherManager.when_getting_watcher;

public class first_time_and_client_is_disconnected : given.a_projection_watcher_manager
{
    IReadModelWatcher<SomeModel> _expectedWatcher;
    IReadModelWatcher<SomeModel> _watcher;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(false);

        _expectedWatcher = Substitute.For<IReadModelWatcher<SomeModel>>();
        _projectionWatcherFactory.Create<SomeModel>(Arg.Any<Action>()).Returns(_expectedWatcher);
    }

    void Because() => _watcher = _manager.GetWatcher<SomeModel>();

    [Fact] void should_return_watcher() => _watcher.ShouldNotBeNull();
    [Fact] void should_return_expected_watcher() => _watcher.ShouldEqual(_expectedWatcher);
    [Fact] void should_not_start_watcher() => _expectedWatcher.DidNotReceive().Start();
}
