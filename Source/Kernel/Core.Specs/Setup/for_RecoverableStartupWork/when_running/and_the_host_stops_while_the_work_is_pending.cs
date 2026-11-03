// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.when_running;

public class and_the_host_stops_while_the_work_is_pending : given.a_recoverable_startup_work
{
    readonly TaskCompletionSource _neverCompletes = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _error;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var run = _work.Run(_item, _ => _neverCompletes.Task, cancellation.Token);
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_complete_without_an_exception() => _error.ShouldBeNull();
    [Fact] void should_not_escalate() => _escalation.Escalations.ShouldBeEmpty();
}
