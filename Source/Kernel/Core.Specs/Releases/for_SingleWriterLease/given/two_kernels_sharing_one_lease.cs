// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.given;

public class two_kernels_sharing_one_lease : Specification
{
    protected a_shared_lease_store Store;
    protected ManualTimeProvider Time;
    protected ChronicleOptions Options;
    protected SingleWriterLeaseKeeper Current;
    protected SingleWriterLeaseKeeper Replacement;
    protected ILifecycleObserver CurrentLifecycle;
    protected ILifecycleObserver ReplacementLifecycle;

    void Establish()
    {
        // Neither kernel loses its lease, so the process exit code is never touched and is not restored here:
        // restoring it would race the lease-loss specifications that assert on it.
        Store = new();
        Time = new();
        Options = new();
        Options.Clustering.SingleWriterLease.Standby = true;
        (Current, CurrentLifecycle) = CreateKernel();
        (Replacement, ReplacementLifecycle) = CreateKernel();
    }

    async Task Destroy()
    {
        await ReplacementLifecycle.OnStop(CancellationToken.None);
        await CurrentLifecycle.OnStop(CancellationToken.None);
        Current.Dispose();
        Replacement.Dispose();
    }

    (SingleWriterLeaseKeeper Keeper, ILifecycleObserver Lifecycle) CreateKernel()
    {
        var keeper = new SingleWriterLeaseKeeper(
            Store,
            Microsoft.Extensions.Options.Options.Create(Options),
            Microsoft.Extensions.Options.Options.Create(new ClusterOptions()),
            Substitute.For<IHostApplicationLifetime>(),
            Substitute.For<IShutdownDrainBudget>(),
            Time,
            Substitute.For<ISingleWriterLeaseFailFast>(),
            Microsoft.Extensions.Options.Options.Create(new HostOptions { ShutdownTimeout = TimeSpan.FromSeconds(10) }),
            Substitute.For<ILogger<SingleWriterLeaseKeeper>>());
        ILifecycleObserver observer = null!;
        var lifecycle = Substitute.For<ISiloLifecycle>();
        lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>()).Returns(call =>
        {
            observer = call.ArgAt<ILifecycleObserver>(2);
            return Substitute.For<IDisposable>();
        });
        keeper.Participate(lifecycle);
        return (keeper, observer);
    }
}
