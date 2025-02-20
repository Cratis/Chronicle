// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionWatcher;

public class when_starting : given.a_watcher
{
    IObservable<ProjectionChangeset> _observable;
    ProjectionWatchRequest _request;

    void Establish()
    {
        _observable = Substitute.For<IObservable<ProjectionChangeset>>();
        _projectionsService.Watch(Arg.Any<ProjectionWatchRequest>()).Returns(_observable);
        _projectionsService.When(_ => _.Watch(Arg.Any<ProjectionWatchRequest>())).Do(_ => _request = _.Arg<ProjectionWatchRequest>());
    }

    void Because() => _watcher.Start();

    [Fact] void should_request_watching() => _projectionsService.Received().Watch(Arg.Any<ProjectionWatchRequest>());
    [Fact] void should_request_watching_with_correct_event_store() => _request.EventStore.ShouldEqual(_eventStoreName.Value);
    [Fact] void should_request_watching_with_correct_projection_id() => _request.ProjectionId.ShouldEqual(_projectionId.Value);
    [Fact] void should_subscribe_to_observable() => _observable.Received().Subscribe(Arg.Any<IObserver<ProjectionChangeset>>());
}
