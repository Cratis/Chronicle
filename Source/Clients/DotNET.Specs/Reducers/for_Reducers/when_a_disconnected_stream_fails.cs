// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_a_disconnected_stream_fails : given.a_reconnecting_reducer
{
    Exception _error;

    async Task Because()
    {
        Disconnect();
        _error = Catch.Exception(() => _streams[0].OnError(new ConnectionUnavailable("disconnected")));
        await ReconnectAndDeliver();
    }

    [Fact] void should_not_access_the_disposed_handler() => _error.ShouldBeNull();
    [Fact] void should_cancel_the_previous_stream() => _originalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_replace_the_handler() => ReferenceEquals(_original, _reducers.GetHandlerFor(typeof(Model))).ShouldBeFalse();
    [Fact] void should_use_a_fresh_stream() => _streams.Count.ShouldEqual(2);
    [Fact] void should_use_the_new_connection() => (_registrations[0].ConnectionId != _registrations[1].ConnectionId).ShouldBeTrue();
    [Fact] async Task should_deliver_after_reconnecting() => (await _result.Task).ReadModelState.ShouldContain("recovered");
    [Fact] void should_not_log_an_error() => HasErrorLog().ShouldBeFalse();
}
