// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerObservers.when_getting_watcher;

public class multiple_times : given.a_reducer_observers
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    IReducerWatcher<MyReadModel> _firstWatcher;
    IReducerWatcher<MyReadModel> _secondWatcher;

    void Establish() => _firstWatcher = _observers.GetWatcher<MyReadModel>();

    void Because() => _secondWatcher = _observers.GetWatcher<MyReadModel>();

    [Fact] void should_return_same_watcher_instance() => _secondWatcher.ShouldEqual(_firstWatcher);
}
