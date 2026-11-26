// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudgetExtensions.when_waiting_within_the_deadline;

public class and_the_deadline_has_already_passed : Specification
{
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _call = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completed = true;

    async Task Because()
    {
        await _deadline.CancelAsync();
        _completed = await _call.Task.CompletesBefore(_deadline.Token);
    }

    [Fact] void should_abandon_the_call_without_waiting() => _completed.ShouldBeFalse();
}
