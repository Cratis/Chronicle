// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_a_disconnected_stream_completes : given.a_reconnecting_reducer
{
    Exception _error;

    async Task Because()
    {
        Disconnect();
        _error = Catch.Exception(_streams[0].OnCompleted);
        await ReconnectAndDeliver();
    }

    [Fact] void should_not_access_the_disposed_handler() => _error.ShouldBeNull();
    [Fact] void should_not_schedule_the_old_handler() => _reconnectWork.ShouldBeEmpty();
    [Fact] void should_use_a_fresh_stream() => _streams.Count.ShouldEqual(2);
    [Fact] async Task should_deliver_after_reconnecting() => (await _result.Task).ReadModelState.ShouldContain("recovered");
    [Fact] void should_not_log_an_error() => HasErrorLog().ShouldBeFalse();
}
