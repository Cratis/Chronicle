// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_resolving_services_disconnects_the_handler : given.a_reconnecting_reducer
{
    async Task Because()
    {
        _streams[0].OnCompleted();
        var disconnected = false;
        _servicesAccessor.Services.Returns(_ =>
        {
            if (!disconnected)
            {
                disconnected = true;
                Disconnect();
            }
            return _services;
        });
        await _reconnectWork.Single()();
        await ReconnectAndDeliver();
    }

    [Fact] void should_cancel_the_previous_stream() => _originalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_open_a_stream_for_the_disposed_handler() => _streams.Count.ShouldEqual(2);
    [Fact] async Task should_deliver_after_reconnecting() => (await _result.Task).ReadModelState.ShouldContain("recovered");
    [Fact] void should_not_log_a_disposed_object_error() => HasErrorLog().ShouldBeFalse();
}
