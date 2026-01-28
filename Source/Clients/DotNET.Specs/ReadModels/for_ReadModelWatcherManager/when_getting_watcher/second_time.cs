// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcherManager.when_getting_watcher;

public class second_time : given.a_projection_watcher_manager
{
    IReadModelWatcher<SomeModel> _expectedWatcher;
    IReadModelWatcher<SomeModel> _firstWatcher;
    IReadModelWatcher<SomeModel> _secondWatcher;

    void Establish()
    {
        _expectedWatcher = Substitute.For<IReadModelWatcher<SomeModel>>();
        _projectionWatcherFactory.Create<SomeModel>(Arg.Any<Action>()).Returns(_expectedWatcher);

        _firstWatcher = _manager.GetWatcher<SomeModel>();
    }

    void Because() => _secondWatcher = _manager.GetWatcher<SomeModel>();

    [Fact] void should_return_watcher_first_time() => _firstWatcher.ShouldNotBeNull();
    [Fact] void should_return_watcher_second_time() => _secondWatcher.ShouldNotBeNull();
    [Fact] void should_return_expected_watcher_first_time() => _firstWatcher.ShouldEqual(_expectedWatcher);
    [Fact] void should_return_expected_watcher_second_time() => _secondWatcher.ShouldEqual(_expectedWatcher);
    [Fact] void should_only_start_watcher_once() => _expectedWatcher.Received(1).Start();
}
