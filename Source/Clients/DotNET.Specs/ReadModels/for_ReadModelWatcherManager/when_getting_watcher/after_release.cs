// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcherManager.when_getting_watcher;

public class after_release : given.a_projection_watcher_manager
{
    Exception _error;

    void Because()
    {
        _manager.Release();
        _error = Catch.Exception(() => _manager.GetWatcher<SomeModel>());
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_not_create_a_watcher() => _projectionWatcherFactory.DidNotReceive().Create<SomeModel>(Arg.Any<Action>());
}
