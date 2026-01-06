// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_both_projection_and_reducer_exist : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    Subject<ProjectionChangeset<MyReadModel>> _projectionObservable;
    Subject<ReducerChangeset<MyReadModel>> _reducerObservable;
    IObservable<ReadModelChangeset<MyReadModel>> _result;

    void Establish()
    {
        _projectionObservable = new Subject<ProjectionChangeset<MyReadModel>>();
        _reducerObservable = new Subject<ReducerChangeset<MyReadModel>>();

        _projections.HasFor<MyReadModel>().Returns(true);
        _projections.Watch<MyReadModel>().Returns(_projectionObservable);

        _reducers.HasFor<MyReadModel>().Returns(true);
        _reducers.Watch<MyReadModel>().Returns(_reducerObservable);
    }

    void Because() => _result = _readModels.Watch<MyReadModel>();

    [Fact] void should_check_if_projection_exists() => _projections.Received(1).HasFor<MyReadModel>();
    [Fact] void should_check_if_reducer_exists() => _reducers.Received(1).HasFor<MyReadModel>();
    [Fact] void should_get_observable_from_projection() => _projections.Received(1).Watch<MyReadModel>();
    [Fact] void should_get_observable_from_reducer() => _reducers.Received(1).Watch<MyReadModel>();
    [Fact] void should_return_observable() => _result.ShouldNotBeNull();
}
