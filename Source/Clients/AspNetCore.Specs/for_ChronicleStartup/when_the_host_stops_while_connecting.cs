// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup;

public class when_the_host_stops_while_connecting : given.a_registered_web_host
{
    Task _startupWait;

    void Establish() => _app.UseCratisChronicle();

    async Task Because()
    {
        // ApplicationStarted deliberately discards this task. Invoke the same connector directly so completion,
        // rather than StopAsync's already-completed task, proves cancellation ended the wait without ending transport.
        var connector = _startup.GetType().GetMethod("ConnectInBackground", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _startupWait = (Task)connector.Invoke(_startup, [_stopping.Token])!;
        await _connectCalled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _stopping.CancelAsync();
        await _startupWait.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_complete_the_startup_wait() => _startupWait.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_wait_for_the_transport_to_finish() => _connectCompletion.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_report_a_startup_failure_for_shutdown() => _logs.Entries.Any(entry => entry.Error is not null).ShouldBeFalse();
    [Fact] void should_not_start_another_connection() => _connection.Received(1).Connect();
}
