// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher.when_starting;

public class when_starting : given.a_watcher
{
    IObservable<ReadModelChangeset> _observable;
    WatchRequest _request;

    void Establish()
    {
        _observable = Substitute.For<IObservable<ReadModelChangeset>>();
        _readModelsService.Watch(Arg.Any<WatchRequest>()).Returns(_observable);
        _readModelsService.When(_ => _.Watch(Arg.Any<WatchRequest>())).Do(_ => _request = _.Arg<WatchRequest>());
    }

    void Because() => _watcher.Start();

    [Fact] void should_request_watching() => _readModelsService.Received().Watch(Arg.Any<WatchRequest>());
    [Fact] void should_request_watching_with_correct_event_store() => _request.EventStore.ShouldEqual(_eventStoreName.Value);
    [Fact] void should_request_watching_with_correct_projection_id() => _request.ReadModelIdentifier.ShouldEqual(typeof(SomeModel).GetReadModelIdentifier().Value);
    [Fact] void should_subscribe_to_observable() => _observable.Received().Subscribe(Arg.Any<IObserver<ReadModelChangeset>>());
}
