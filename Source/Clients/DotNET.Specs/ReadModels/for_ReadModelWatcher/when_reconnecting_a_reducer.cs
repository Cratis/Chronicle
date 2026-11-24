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
    Task _initialSubscription;
    Task _afterDisconnect;
    Task _afterReconnect;
    bool _readyBeforeAcknowledgment;

    void Establish()
    {
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        var handler = Substitute.For<IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequenceId.Outbox);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(handler);
        _readModelsService.Watch(Arg.Do<Contracts.ReadModels.WatchRequest>(request => _sequences.Add(request.EventSequenceId))).Returns(_frames);
        _watcher.Start();
        _initialSubscription = _watcher.Subscribed;
        _frames.OnNext(new() { Subscribed = true });
    }

    void Because()
    {
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _afterDisconnect = _watcher.Subscribed;
        var replacement = Substitute.For<IReducerHandler>();
        replacement.EventSequenceId.Returns(EventSequenceId.Inbox);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(replacement);
        _lifecycle.OnConnected += Raise.Event<Connected>();
        _afterReconnect = _watcher.Subscribed;
        _readyBeforeAcknowledgment = _afterReconnect.IsCompleted;
        _frames.OnNext(new() { Subscribed = true });
    }

    void Destroy()
    {
        _watcher.Dispose();
        _frames.Dispose();
    }

    [Fact] void should_confirm_the_initial_subscription() => _initialSubscription.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_replace_readiness_on_disconnect() => ReferenceEquals(_initialSubscription, _afterDisconnect).ShouldBeFalse();
    [Fact] void should_keep_the_pending_signal_when_reconnecting() => _afterReconnect.ShouldEqual(_afterDisconnect);
    [Fact] void should_wait_for_a_new_kernel_acknowledgment() => _readyBeforeAcknowledgment.ShouldBeFalse();
    [Fact] void should_confirm_the_reconnected_subscription() => _afterReconnect.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_resolve_the_handler_again_on_start() => _sequences.ShouldEqual([EventSequenceId.Outbox.Value, EventSequenceId.Inbox.Value]);
}
