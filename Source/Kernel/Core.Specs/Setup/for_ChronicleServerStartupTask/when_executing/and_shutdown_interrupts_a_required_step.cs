// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_shutdown_interrupts_a_required_step : given.a_startup_task
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _failure;

    void Establish() => _patchManager.ApplyPatches().Returns(async _ =>
    {
        _entered.SetResult();
        await _release.Task;
    });

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var startup = Execute(cancellation.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();
        try
        {
            _failure = await Catch.Exception(() => startup.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            _release.SetResult();
        }
    }

    [Fact] void should_surface_shutdown() => _failure.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] async Task should_not_continue_to_the_next_required_step() => await _systemNamespaces.DidNotReceive().EnsureDefault();
    [Fact] void should_not_report_bootstrap_complete() => _bootstrapClientsEnsured.ShouldBeFalse();
}
