// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_a_reducer_stream_completes_before_acknowledgment : given.a_watcher
{
    void Establish()
    {
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        var handler = Substitute.For<Reducers.IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(handler);
        _readModelsService.Watch(Arg.Any<Contracts.ReadModels.WatchRequest>()).Returns(
            Observable.Empty<Contracts.ReadModels.ReadModelChangeset>());
    }

    void Because() => _watcher.Start();

    [Fact] void should_surface_the_completion_instead_of_leaving_readiness_pending() => _watcher.Subscribed.IsCanceled.ShouldBeTrue();

    void Destroy() => _watcher.Dispose();
}
