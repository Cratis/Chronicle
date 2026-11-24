// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_a_confirmed_reducer_stream_fails : given.a_watcher
{
    readonly Subject<Contracts.ReadModels.ReadModelChangeset> _frames = new();
    readonly InvalidOperationException _error = new("Confirmed stream failed");
    Task _confirmed;

    void Establish()
    {
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        var handler = Substitute.For<Reducers.IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(handler);
        _readModelsService.Watch(Arg.Any<Contracts.ReadModels.WatchRequest>()).Returns(_frames);
        _watcher.Start();
        _frames.OnNext(new() { Subscribed = true });
        _confirmed = _watcher.Subscribed;
    }

    void Because() => _frames.OnError(_error);

    [Fact] void should_preserve_the_previous_acknowledgment() => _confirmed.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_no_longer_report_the_failed_stream_as_ready() => _watcher.Subscribed.IsFaulted.ShouldBeTrue();
    [Fact] void should_surface_the_stream_failure() => _watcher.Subscribed.Exception.InnerException.ShouldEqual(_error);

    void Destroy()
    {
        _watcher.Dispose();
        _frames.Dispose();
    }
}
