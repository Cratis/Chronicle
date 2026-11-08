// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect.given;

public abstract class a_registration_crossing_a_connection_change : for_Reactors.given.a_registered_delegate
{
    protected readonly List<Func<Task>> _reconnectWork = [];
    protected IServices _replacement;
    protected Exception _error;
    protected int _servicesReads;
    readonly TaskCompletionSource _resolved = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IServices _current;
    int _gateNext;

    protected override TimeSpan ReconnectDelay => TimeSpan.Zero;
    protected override Func<Func<Task>, Task> ScheduleReconnectWork => work =>
    {
        _reconnectWork.Add(work);
        return Task.CompletedTask;
    };

    void Establish()
    {
        _connectionLifecycle.IsConnected.Returns(true);
        _eventTypes.GetEventTypeFor(typeof(OrderPlaced)).Returns(new EventType("orders", 1));
        _replacement = Substitute.For<IServices>();
        _replacement.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Never<EventsToObserve>());
        _current = _services;
        _servicesAccessor.Services.Returns(_ =>
        {
            Interlocked.Increment(ref _servicesReads);
            var resolved = _current;
            if (Interlocked.Exchange(ref _gateNext, 0) == 1)
            {
                _resolved.TrySetResult();
                _resume.Task.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
            return resolved;
        });
        _gateNext = 1;
    }

    protected async Task CrossConnectionChange()
    {
        var registration = Task.Run(async () => _error = await Catch.Exception(RegisterAcrossConnectionChange));
        try
        {
            await _resolved.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await WhileServicesAreResolved();
        }
        finally
        {
            _resume.TrySetResult();
            await registration.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    protected abstract Task RegisterAcrossConnectionChange();

    protected virtual async Task WhileServicesAreResolved()
    {
        _connectionLifecycle.IsConnected.Returns(false);
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _current = _replacement;
        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"replacement-connection");
        _connectionLifecycle.IsConnected.Returns(true);
        await _reactors.RegisterAgainAfterReconnect();
    }

    protected class AnotherReactor : IReactor;
    protected record OrderPlaced(string OrderNumber);
}
