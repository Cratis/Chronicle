// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientStartup;

public class when_waiting_for_completion_and_the_host_is_stopping_during_pending_startup : given.a_chronicle_client_startup
{
    bool _completed;

    void Establish() => _connection.Connect().Returns(_ =>
    {
        _connectCalled.TrySetResult();
        return new TaskCompletionSource().Task;
    });

    async Task Because()
    {
        await StartAndSignalStarted();
        await _connectCalled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _stopping.CancelAsync();
        await _startup.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        _completed = _startup.Completion.IsCompletedSuccessfully;
    }

    [Fact] void should_complete() => _completed.ShouldBeTrue();
}
