// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_shutdown_cancels_transient_retry_backoff : given.a_startup_task
{
    readonly CancellationTokenSource _cancellation = new();
    Exception? _failure;

    void Establish() => _patchManager.ApplyPatches().Returns(async _ =>
    {
        await _cancellation.CancelAsync();
        throw new TimeoutException("Planted transient during shutdown");
    });

    async Task Because() => _failure = await Catch.Exception(() => Execute(_cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_surface_cancellation_instead_of_exhausting_retries() => _failure.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] async Task should_not_retry_after_shutdown() => await _patchManager.Received(1).ApplyPatches();
    [Fact] void should_not_report_bootstrap_complete() => _bootstrapClientsEnsured.ShouldBeFalse();
}
