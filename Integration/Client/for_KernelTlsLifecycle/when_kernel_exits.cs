// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Docker.DotNet;
using Docker.DotNet.Models;
using OwnedFixture = Cratis.Chronicle.Integration.for_KernelTlsLifecycle.when_tls_is_refused.OwnedFixture;

namespace Cratis.Chronicle.Integration.for_KernelTlsLifecycle;

public class when_kernel_exits : Cratis.Specifications.Specification
{
    [Fact] public async Task should_join_the_pending_native_reset_and_isolate_a_fresh_fixture()
    {
        await using var owner = new OwnedFixture();
        owner.Construct();
        using var client = OwnedFixture.Client();
        Task? reset = null;
        Task? native = null;
        try
        {
            using var pauseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var pause = client.Containers.PauseContainerAsync(owner.Kernel.Id, pauseTimeout.Token);
            ObserveDelayedFault(pause);
            await pause.WaitAsync(TimeSpan.FromSeconds(15));
            var state = await client.Containers.InspectContainerAsync(owner.Kernel.Id).WaitAsync(TimeSpan.FromSeconds(10));
            state.State.Paused.ShouldBeTrue();
            reset = owner.Fixture!.ResetForLifecycleProofAsync();
            native = await owner.Issued.Task.WaitAsync(TimeSpan.FromSeconds(10));
            native.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            // Kill is issued before unpause, but is not awaited while the process remains frozen.
            try
            {
                await KillAndUnpause(client, owner.Kernel.Id);
            }
            finally
            {
                if (reset is not null)
                {
                    await Catch.Exception(async () => await reset.WaitAsync(TimeSpan.FromSeconds(15)));
                    reset.IsCompleted.ShouldBeTrue();
                }
                native?.IsCompleted.ShouldBeTrue();
            }
        }
        var failure = await Catch.Exception(async () => await reset!.WaitAsync(TimeSpan.FromSeconds(15)));
        Assert.IsType<KernelProcessExited>(failure);
        native!.IsCompleted.ShouldBeTrue();
        var issued = owner.IssuedCount;
        var later = await Catch.Exception(owner.Fixture!.ResetForLifecycleProofAsync);
        later.ShouldEqual(failure);
        owner.IssuedCount.ShouldEqual(issued);
        await using var fresh = new OwnedFixture();
        fresh.Construct();
        fresh.Kernel.Id.ShouldNotEqual(owner.Kernel.Id);
        fresh.Material!.Hash.ShouldNotEqual(owner.Material!.Hash);
        fresh.Material.Path.ShouldNotEqual(owner.Material.Path);
        await fresh.Fixture!.ResetForLifecycleProofAsync().WaitAsync(TimeSpan.FromSeconds(30));
        await owner.DisposeFixtureAsync();
        await fresh.DisposeFixtureAsync();
        await owner.AssertGoneAsync();
        await fresh.AssertGoneAsync();
        Directory.Exists(owner.MaterialDirectory).ShouldBeFalse();
        Directory.Exists(fresh.MaterialDirectory).ShouldBeFalse();
    }

    static async Task KillAndUnpause(DockerClient client, string id)
    {
        using var killTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Task? kill = null;
        try
        {
            try
            {
                kill = client.Containers.KillContainerAsync(id, new ContainerKillParameters { Signal = "SIGKILL" }, killTimeout.Token);
                ObserveDelayedFault(kill);
            }
            finally
            {
                // Recovery has its own token even when pause completion or kill issuance is ambiguous.
                await UnpauseOwnedContainer(client, id);
            }
        }
        finally
        {
            if (kill is not null) await kill.WaitAsync(TimeSpan.FromSeconds(15));
        }
        using var exitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var exit = await client.Containers.WaitContainerAsync(id, exitTimeout.Token).WaitAsync(TimeSpan.FromSeconds(15));
        exit.StatusCode.ShouldNotEqual(0);
    }

    static async Task UnpauseOwnedContainer(DockerClient client, string id)
    {
        using var unpauseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await client.Containers.UnpauseContainerAsync(id, unpauseTimeout.Token).WaitAsync(TimeSpan.FromSeconds(15));
        }
        catch (DockerApiException)
        {
            // A completed kill may already have unpaused and stopped this same owned container.
            using var inspectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var state = await client.Containers.InspectContainerAsync(id, inspectTimeout.Token).WaitAsync(TimeSpan.FromSeconds(15));
            state.State.Running.ShouldBeFalse();
        }
    }

    /// <summary>
    /// Observes native faults that may arrive after a watchdog expires; a watchdog only bounds the wait.
    /// </summary>
    /// <param name="operation">The native operation to observe.</param>
    static void ObserveDelayedFault(Task operation) => _ = operation.ContinueWith(
        completed => _ = completed.Exception,
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);
}
