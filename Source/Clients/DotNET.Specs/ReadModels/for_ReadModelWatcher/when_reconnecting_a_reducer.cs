// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_reconnecting_a_reducer : given.a_watcher
{
    readonly Subject<Contracts.ReadModels.ReadModelChangeset> _frames = new();
    readonly List<string> _sequences = [];

    void Establish()
    {
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        var handler = Substitute.For<IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequenceId.Outbox);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(handler);
        _readModelsService.Watch(Arg.Do<Contracts.ReadModels.WatchRequest>(request => _sequences.Add(request.EventSequenceId))).Returns(_frames);
        _watcher.Start();
    }

    void Because()
    {
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        var replacement = Substitute.For<IReducerHandler>();
        replacement.EventSequenceId.Returns(EventSequenceId.Inbox);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(replacement);
        _lifecycle.OnConnected += Raise.Event<Connected>();
    }

    void Destroy()
    {
        _watcher.Dispose();
        _frames.Dispose();
    }

    [Fact] void should_resolve_the_handler_again_on_start() => _sequences.ShouldEqual([EventSequenceId.Outbox.Value, EventSequenceId.Inbox.Value]);
}
