// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_a_handler_is_replaced_during_reconnect : given.a_reconnecting_reducer
{
    async Task Because()
    {
        _streams[0].OnCompleted();
        Disconnect();
        foreach (var work in _reconnectWork.ToArray()) await work();
        await ReconnectAndDeliver();
    }

    [Fact] void should_have_queued_a_reconnect() => _reconnectWork.Count.ShouldEqual(1);
    [Fact] void should_not_reuse_the_old_handler() => _streams.Count.ShouldEqual(2);
    [Fact] async Task should_deliver_after_reconnecting() => (await _result.Task).ReadModelState.ShouldContain("recovered");
    [Fact] void should_not_log_a_disposed_object_error() => HasErrorLog().ShouldBeFalse();
}
