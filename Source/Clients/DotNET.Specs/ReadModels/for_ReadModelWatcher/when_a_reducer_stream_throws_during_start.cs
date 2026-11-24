// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_a_reducer_stream_throws_during_start : given.a_watcher
{
    readonly InvalidOperationException _error = new("Stream could not start");

    void Establish()
    {
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        var handler = Substitute.For<Reducers.IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(handler);
        _readModelsService.Watch(Arg.Any<Contracts.ReadModels.WatchRequest>()).Returns(_ => throw _error);
    }

    void Because() => _watcher.Start();

    [Fact] void should_surface_the_failure_in_readiness() => _watcher.Subscribed.IsFaulted.ShouldBeTrue();
    [Fact] void should_preserve_the_failure() => _watcher.Subscribed.Exception.InnerException.ShouldEqual(_error);

    void Destroy() => _watcher.Dispose();
}
