// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudgetExtensions.when_waiting_within_the_deadline;

public class and_the_call_is_still_running_when_the_wait_resumes : Specification
{
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _call = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completed = true;

    async Task Because()
    {
        var waiting = _call.Task.CompletesBefore(_deadline.Token);
        await _deadline.CancelAsync();
        _completed = await waiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_abandon_the_call() => _completed.ShouldBeFalse();
}
