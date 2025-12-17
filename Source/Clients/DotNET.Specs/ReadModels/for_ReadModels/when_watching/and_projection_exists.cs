// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_projection_exists : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    Subject<ProjectionChangeset<MyReadModel>> _projectionObservable;
    IObservable<ReadModelChangeset<MyReadModel>> _result;

    void Establish()
    {
        _projectionObservable = new Subject<ProjectionChangeset<MyReadModel>>();
        _projections.HasFor(typeof(MyReadModel)).Returns(true);
        _projections.Watch<MyReadModel>().Returns(_projectionObservable);
    }

    void Because() => _result = _readModels.Watch<MyReadModel>();

    [Fact] void should_check_if_projection_exists() => _projections.Received(1).HasFor(typeof(MyReadModel));
    [Fact] void should_get_observable_from_projection() => _projections.Received(1).Watch<MyReadModel>();
    [Fact] void should_return_observable() => _result.ShouldNotBeNull();
}
