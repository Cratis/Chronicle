// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcherManager;

public class when_releasing : given.a_projection_watcher_manager
{
    IReadModelWatcher<when_getting_watcher.SomeModel> _watcher;

    void Establish()
    {
        _watcher = Substitute.For<IReadModelWatcher<when_getting_watcher.SomeModel>>();
        _projectionWatcherFactory.Create<when_getting_watcher.SomeModel>(Arg.Any<Action>()).Returns(_watcher);
    }

    void Because()
    {
        _manager.GetWatcher<when_getting_watcher.SomeModel>();
        _manager.Release();
    }

    [Fact] void should_stop_the_watcher() => _watcher.Received(1).Stop();
}
