// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect.given;

public abstract class a_registration_crossing_a_connection_change : a_registered_reducer
{
    protected IServices _replacement;
    protected Exception _error;
    protected int _servicesReads;
    readonly TaskCompletionSource _resolved = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IServices _current;
    int _gateNext;

    void Establish()
    {
        _replacement = Substitute.For<IServices>();
        _replacement.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
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
        _lifecycle.IsConnected.Returns(false);
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _current = _replacement;
        _lifecycle.ConnectionId.Returns((ConnectionId)"replacement-connection");
        _lifecycle.IsConnected.Returns(true);
        await _reducers.RegisterAgainAfterReconnect();
    }
}
