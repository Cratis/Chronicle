// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.SingleWriter;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.given;

public class a_keeper : Specification
{
    protected ISingleWriterLeaseStorage Storage;
    protected ManualTimeProvider Time;
    protected SingleWriterLeaseKeeper Keeper;
    protected ILifecycleObserver Observer;
    protected ISiloLifecycle Lifecycle;
    protected IHostApplicationLifetime Lifetime;
    protected ISingleWriterLeaseFailFast FailFast;
    protected IShutdownDrainBudget Drain;
    protected ChronicleOptions Options;
    int _exitCode;

    void Establish()
    {
        _exitCode = Environment.ExitCode;
        Storage = Substitute.For<ISingleWriterLeaseStorage>();
        Time = new();
        Lifetime = Substitute.For<IHostApplicationLifetime>();
        Drain = Substitute.For<IShutdownDrainBudget>();
        FailFast = Substitute.For<ISingleWriterLeaseFailFast>();
        Options = new();
        Storage.TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>()).Returns(new SingleWriterLeaseAttempt(true, null));
        Storage.Renew(Arg.Any<SingleWriterLeaseOwner>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(true);
        CreateKeeper();
    }

    protected void CreateKeeper()
    {
        Keeper?.Dispose();
        Keeper = new(
            Storage,
            Microsoft.Extensions.Options.Options.Create(Options),
            Microsoft.Extensions.Options.Options.Create(new ClusterOptions()),
            Lifetime,
            Drain,
            Time,
            FailFast,
            Microsoft.Extensions.Options.Options.Create(new HostOptions { ShutdownTimeout = TimeSpan.FromSeconds(10) }),
            Substitute.For<ILogger<SingleWriterLeaseKeeper>>());
        Lifecycle = Substitute.For<ISiloLifecycle>();
        Lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>()).Returns(call =>
        {
            Observer = call.ArgAt<ILifecycleObserver>(2);
            return Substitute.For<IDisposable>();
        });
        Keeper.Participate(Lifecycle);
    }

    async Task Destroy()
    {
        await Observer.OnStop(CancellationToken.None);
        Keeper.Dispose();
        Environment.ExitCode = _exitCode;
    }
}
