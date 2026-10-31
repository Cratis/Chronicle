// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering;

public class and_observation_stream_completes : given.all_dependencies
{
    readonly Subject<ReduceOperationMessage> _firstStream = new();
    readonly Subject<ReduceOperationMessage> _replacementStream = new();
    readonly CompositeDisposable _outgoingSubscriptions = new();
    readonly CancellationTokenSource _handlerCancellation = new();
    readonly ConcurrentQueue<RegisterReducer> _registrations = new();
    readonly TaskCompletionSource<RegisterReducer> _firstRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<RegisterReducer> _secondRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _replacementSubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    RegisterReducer[] _registrationsAtRecovery = [];
    RegisterReducer? _first;
    RegisterReducer? _second;
    bool _registrationResumed;
    int _observeCalls;

    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(Connections.ConnectionId.New());
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(CompletingReducer));
        handler.ReadModelType.Returns(typeof(ReadModel));
        handler.Id.Returns((ReducerId)"completing-reducer");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([new EventType("person-registered", 2)]);
        handler.IsActive.Returns(true);
        handler.CancellationToken.Returns(_handlerCancellation.Token);
        _handlersByModelType[typeof(ReadModel)] = handler;
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            var attempt = Interlocked.Increment(ref _observeCalls);
            _outgoingSubscriptions.Add(call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message =>
            {
                var registration = message.Content.Value0!;
                _registrations.Enqueue(registration);
                if (attempt == 1)
                {
                    _firstRegistration.TrySetResult(registration);
                }
                else if (attempt == 2)
                {
                    _secondRegistration.TrySetResult(registration);
                }
            }));
            return attempt == 1 ? _firstStream : Observable.Create<ReduceOperationMessage>(observer =>
            {
                var subscription = _replacementStream.Subscribe(observer);
                _replacementSubscribed.TrySetResult();
                return subscription;
            });
        });
    }

    async Task Because()
    {
        await _reducers.Register();
        _first = await _firstRegistration.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _firstStream.OnCompleted();
        try
        {
            _second = await _secondRegistration.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await _replacementSubscribed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _registrationResumed = true;
        }
        catch (TimeoutException)
        {
            _registrationResumed = false;
        }
        _registrationsAtRecovery = _registrations.ToArray();
    }

    void Destroy()
    {
        _handlerCancellation.Cancel();
        _outgoingSubscriptions.Dispose();
        _firstStream.Dispose();
        _replacementStream.Dispose();
        _handlerCancellation.Dispose();
    }

    [Fact] void should_register_again_when_the_stream_completes() => _registrationResumed.ShouldBeTrue();
    [Fact] void should_capture_exactly_two_registrations_at_recovery() => _registrationsAtRecovery.Length.ShouldEqual(2);
    [Fact] void should_subscribe_to_the_replacement_stream() => _replacementSubscribed.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_leave_the_handler_uncancelled() => _handlerCancellation.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_not_reconnect_the_whole_client() => _eventStore.Connection.DidNotReceive().RequestReconnect();

    [Fact]
    void should_retain_the_registration_metadata()
    {
        _second.ShouldNotBeNull();
        var first = _first!;
        var second = _second!;
        second.ConnectionId.ShouldEqual(first.ConnectionId);
        second.Reducer.ReducerId.ShouldEqual(first.Reducer.ReducerId);
        second.Reducer.EventSequenceId.ShouldEqual(first.Reducer.EventSequenceId);
        second.Reducer.EventTypes.Select(type => (type.EventType.Id, type.EventType.Generation))
            .ShouldContainOnly(first.Reducer.EventTypes.Select(type => (type.EventType.Id, type.EventType.Generation)).ToArray());
    }

    class CompletingReducer;
    record ReadModel;
}
