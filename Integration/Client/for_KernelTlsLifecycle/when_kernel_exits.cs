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
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await client.Containers.PauseContainerAsync(owner.Kernel.Id, timeout.Token).WaitAsync(TimeSpan.FromSeconds(15));
        Task? reset = null;
        Task? native = null;
        try
        {
            var state = await client.Containers.InspectContainerAsync(owner.Kernel.Id).WaitAsync(TimeSpan.FromSeconds(10));
            state.State.Paused.ShouldBeTrue();
            reset = owner.Fixture!.ResetForLifecycleProofAsync();
            native = await owner.Issued.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var winner = await Task.WhenAny(native, Task.Delay(250));
            ReferenceEquals(winner, native).ShouldBeFalse();
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
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var kill = client.Containers.KillContainerAsync(id, new ContainerKillParameters { Signal = "SIGKILL" }, timeout.Token);
        try
        {
            await client.Containers.UnpauseContainerAsync(id, timeout.Token).WaitAsync(TimeSpan.FromSeconds(15));
        }
        catch (DockerApiException)
        {
            // A completed kill may already have unpaused and stopped this same owned container.
            var state = await client.Containers.InspectContainerAsync(id).WaitAsync(TimeSpan.FromSeconds(10));
            state.State.Running.ShouldBeFalse();
        }
        await kill.WaitAsync(TimeSpan.FromSeconds(15));
        var exit = await client.Containers.WaitContainerAsync(id, timeout.Token).WaitAsync(TimeSpan.FromSeconds(15));
        exit.StatusCode.ShouldNotEqual(0);
    }
}
