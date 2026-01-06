// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_reducer_exists : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    Subject<ReducerChangeset<MyReadModel>> _reducerObservable;
    IObservable<ReadModelChangeset<MyReadModel>> _result;

    void Establish()
    {
        _reducerObservable = new Subject<ReducerChangeset<MyReadModel>>();
        _reducers.HasFor<MyReadModel>().Returns(true);
        _reducers.Watch<MyReadModel>().Returns(_reducerObservable);
    }

    void Because() => _result = _readModels.Watch<MyReadModel>();

    [Fact] void should_check_if_projection_exists() => _projections.Received(1).HasFor<MyReadModel>();
    [Fact] void should_check_if_reducer_exists() => _reducers.Received(1).HasFor<MyReadModel>();
    [Fact] void should_get_observable_from_reducer() => _reducers.Received(1).Watch<MyReadModel>();
    [Fact] void should_return_observable() => _result.ShouldNotBeNull();
}
