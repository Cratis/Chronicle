// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using OwnedFixture = Cratis.Chronicle.Integration.for_KernelTlsLifecycle.when_tls_is_refused.OwnedFixture;

namespace Cratis.Chronicle.Integration.for_KernelTlsLifecycle;

public class when_reset_or_disposal_fails : Cratis.Specifications.Specification
{
    [Fact] public async Task should_preserve_the_reset_failure_when_cleanup_fails()
    {
        await using var owner = new OwnedFixture();
        owner.Construct();
        await owner.Fixture!.ResetForLifecycleProofAsync().WaitAsync(TimeSpan.FromSeconds(30));
        owner.Pin = _ => new string('0', 64);

        // Preserve the fixture's existing two-minute retry deadline; this is not a synthetic reset task.
        var resetFailure = await Catch.Exception(async () => await owner.Fixture.ResetForLifecycleProofAsync().WaitAsync(TimeSpan.FromSeconds(150)));
        Assert.IsType<RpcException>(resetFailure);
        var cleanup = new IOException("owned kernel disposal not confirmed");
        owner.Dispose = (container, realDispose) => ReferenceEquals(container, owner.Kernel)
            ? ValueTask.FromException(cleanup)
            : realDispose();
        var primary = await XUnit.Integration.ChronicleFixture.Cleanup(owner.Fixture.DisposeAsync, resetFailure);
        primary.ShouldEqual(resetFailure);
        primary!.Data["FixtureCleanupFailure"].ShouldEqual(cleanup);
        File.Exists(owner.Material!.Path).ShouldBeTrue();
        owner.KernelStopConfirmed.ShouldBeFalse();
        owner.Dispose = null;
        await owner.DisposeFixtureAsync();
        owner.KernelStopConfirmed.ShouldBeTrue();
        await owner.AssertGoneAsync();
        Directory.Exists(owner.MaterialDirectory).ShouldBeFalse();
    }

    [Fact] public async Task should_retain_material_until_disposal_is_confirmed()
    {
        await using var owner = new OwnedFixture();
        owner.Construct();
        await owner.Fixture!.ResetForLifecycleProofAsync().WaitAsync(TimeSpan.FromSeconds(30));
        var cleanup = new IOException("owned kernel disposal not confirmed");
        owner.Dispose = (container, realDispose) => ReferenceEquals(container, owner.Kernel)
            ? ValueTask.FromException(cleanup)
            : realDispose();
        var failure = await Catch.Exception(async () => await owner.Fixture.DisposeAsync());
        failure.ShouldEqual(cleanup);
        File.Exists(owner.Material!.Path).ShouldBeTrue();
        owner.KernelStopConfirmed.ShouldBeFalse();
        using (var client = OwnedFixture.Client())
        {
            var state = await client.Containers.InspectContainerAsync(owner.Kernel.Id).WaitAsync(TimeSpan.FromSeconds(10));
            state.State.Running.ShouldBeTrue();
        }
        owner.Dispose = null;
        await owner.DisposeFixtureAsync();
        owner.KernelStopConfirmed.ShouldBeTrue();
        await owner.AssertGoneAsync();
        Directory.Exists(owner.MaterialDirectory).ShouldBeFalse();
    }
}
