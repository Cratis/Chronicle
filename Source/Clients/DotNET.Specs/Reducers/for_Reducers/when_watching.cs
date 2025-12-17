// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_watching : given.all_dependencies
{
    public class MyReadModel
    {
        public string Name { get; set; }
    }

    IObservable<ReducerChangeset<MyReadModel>> _result;

    void Because() => _result = _reducers.Watch<MyReadModel>();

    [Fact] void should_return_observable() => _result.ShouldNotBeNull();
    [Fact] void should_get_watcher_from_observers() => _reducerObservers.Received(1).GetWatcher<MyReadModel>();
}
