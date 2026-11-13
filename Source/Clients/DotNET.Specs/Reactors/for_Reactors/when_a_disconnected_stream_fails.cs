// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_a_disconnected_stream_fails : given.a_registered_delegate
{
    Subject<EventsToObserve> _previousStream;
    CancellationToken _previousToken;
    Exception _error;

    void Establish()
    {
        _previousStream = _observed;
        _previousToken = _handler.CancellationToken;
    }

    async Task Because()
    {
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _error = Catch.Exception(() => _previousStream.OnError(new ConnectionUnavailable("disconnected")));
        _observed = new();
        await _reactors.Register();
        _release.TrySetResult();
        Deliver();
        await _result.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_not_access_the_disposed_handler() => _error.ShouldBeNull();
    [Fact] void should_cancel_the_previous_stream() => _previousToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_replace_the_handler() => ReferenceEquals(_handler, _reactors.GetHandlerById("bridge")).ShouldBeFalse();
    [Fact] void should_register_again() => _definitions.Count.ShouldEqual(2);
    [Fact] void should_deliver_after_reconnecting() => _received.Task.IsCompletedSuccessfully.ShouldBeTrue();

    void Destroy()
    {
        _reactors.Release();
        _previousStream.Dispose();
        _observed.Dispose();
    }
}
