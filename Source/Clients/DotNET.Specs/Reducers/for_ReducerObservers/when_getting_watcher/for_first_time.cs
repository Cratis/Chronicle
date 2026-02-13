// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerObservers.when_getting_watcher;

public class for_first_time : given.a_reducer_observers
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    IReducerWatcher<MyReadModel> _result;

    void Because() => _result = _observers.GetWatcher<MyReadModel>();

    [Fact] void should_return_a_watcher() => _result.ShouldNotBeNull();
    [Fact] void should_return_reducer_watcher_instance() => _result.ShouldBeOfExactType<ReducerWatcher<MyReadModel>>();
}
