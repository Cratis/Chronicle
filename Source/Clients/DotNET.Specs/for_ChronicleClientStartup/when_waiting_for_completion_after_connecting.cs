// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_waiting_for_completion_after_connecting : given.a_chronicle_client_startup
{
    TaskCompletionSource _release;
    bool _completedBeforeStart;
    bool _completedWhileConnecting;
    bool _completedAfterStart;

    void Establish()
    {
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _connection.Connect().Returns(_ =>
        {
            _connectCalled.TrySetResult();
            return _release.Task;
        });
    }

    async Task Because()
    {
        _completedBeforeStart = _startup.Completion.IsCompleted;
        await StartAndSignalStarted();
        await _connectCalled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _completedWhileConnecting = _startup.Completion.IsCompleted;
        _release.SetResult();
        await _startup.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        _completedAfterStart = _startup.Completion.IsCompletedSuccessfully;
    }

    [Fact] void should_not_be_complete_before_the_host_has_started() => _completedBeforeStart.ShouldBeFalse();
    [Fact] void should_not_be_complete_while_connect_is_pending() => _completedWhileConnecting.ShouldBeFalse();
    [Fact] void should_complete_after_connecting() => _completedAfterStart.ShouldBeTrue();
    [Fact] void should_have_connected() => _connection.Received(1).Connect();
}
