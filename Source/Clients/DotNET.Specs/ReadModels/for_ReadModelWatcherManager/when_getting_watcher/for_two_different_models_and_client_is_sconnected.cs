// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcherManager.when_getting_watcher;

public class for_two_different_models_and_client_is_sconnected : given.a_projection_watcher_manager
{
    IReadModelWatcher<SomeModel> _expectedWatcherForSomeModel;
    IReadModelWatcher<SomeOtherModel> _expectedWatcherForSomeOtherModel;
    IReadModelWatcher<SomeModel> _watcherForSomeModel;
    IReadModelWatcher<SomeOtherModel> _watcherForSomeOtherModel;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(true);

        _expectedWatcherForSomeModel = Substitute.For<IReadModelWatcher<SomeModel>>();
        _expectedWatcherForSomeOtherModel = Substitute.For<IReadModelWatcher<SomeOtherModel>>();
        _projectionWatcherFactory.Create<SomeModel>(Arg.Any<Action>()).Returns(_expectedWatcherForSomeModel);
        _projectionWatcherFactory.Create<SomeOtherModel>(Arg.Any<Action>()).Returns(_expectedWatcherForSomeOtherModel);
    }

    void Because()
    {
        _watcherForSomeModel = _manager.GetWatcher<SomeModel>();
        _watcherForSomeOtherModel = _manager.GetWatcher<SomeOtherModel>();
    }

    [Fact] void should_return_watcher_for_some_model() => _watcherForSomeModel.ShouldNotBeNull();
    [Fact] void should_return_watcher_for_some_other_model() => _watcherForSomeOtherModel.ShouldNotBeNull();
    [Fact] void should_return_expected_watcher_for_some_model() => _watcherForSomeModel.ShouldEqual(_expectedWatcherForSomeModel);
    [Fact] void should_return_expected_watcher_for_some_other_model() => _watcherForSomeOtherModel.ShouldEqual(_expectedWatcherForSomeOtherModel);
    [Fact] void should_not_start_watcher_for_some_model() => _expectedWatcherForSomeModel.Received(1).Start();
    [Fact] void should_not_start_watcher_for_some_other_model() => _expectedWatcherForSomeOtherModel.Received(1).Start();
}
