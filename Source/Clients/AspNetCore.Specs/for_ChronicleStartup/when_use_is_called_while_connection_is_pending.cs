// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup;

public class when_use_is_called_while_connection_is_pending : given.a_registered_web_host
{
    bool _startupReturned;

    void Establish()
    {
        _app.UseCratisChronicle();
        _app.UseCratisChronicle();
    }

    async Task Because()
    {
        await SignalStarted();
        try
        {
            await _startedSignal.WaitAsync(TimeSpan.FromSeconds(10));
            _startupReturned = true;
        }
        catch (TimeoutException)
        {
            // The pending connection is released by the context cleanup even when startup blocks.
            _startupReturned = false;
        }
    }

    [Fact] void should_return_from_startup_before_connection_completes() => _startupReturned.ShouldBeTrue();
    [Fact] void should_leave_the_connection_pending() => _connectCompletion.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_connect_once() => _connection.Received(1).Connect();
    [Fact] void should_get_the_event_store_once() => _client.Received(1).GetEventStore("the-store");
}
