// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Integration.Clustering;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Kernel.Integration.Setup.for_background_startup;

public class BackgroundStartupFixture : ClusteringFixture
{
    public const string EventStore = Constants.EventStore;
    public static readonly TimeSpan OperationDeadline = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan HoldDeadline = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan BarrierDeadline = TimeSpan.FromSeconds(30);
    object _startup = null!;
    bool _enableStartup;

    public StartupStorageGate Gate { get; } = new();
    public Task RecoverableWork => (Task)_startup.GetType()
        .GetProperty("RecoverableWork", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_startup)!;

    protected override short SiloCount => 1;
    protected override Task WarmUpAsync() => Task.CompletedTask;

    public void EnableBackgroundStartup()
    {
        Gate.Arm();
        _enableStartup = true;
    }

    protected override void ConfigureFixtureServices(IServiceCollection services)
    {
        var descriptor = services.Last(service => service.ServiceType == typeof(IStorage));
        services.AddSingleton<IStorage>(provider =>
        {
            var storage = descriptor.ImplementationInstance as IStorage
                ?? descriptor.ImplementationFactory?.Invoke(provider) as IStorage
                ?? (IStorage)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
            return Gate.Wrap(storage);
        });
        if (!_enableStartup) return;

        var startupType = typeof(Configuration.ChronicleOptions).Assembly
            .GetType("Orleans.Hosting.ChronicleServerStartupTask", throwOnError: true)!;
        services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(provider =>
        {
            var participant = ActivatorUtilities.CreateInstance(provider, startupType);
            _startup = participant;
            return (ILifecycleParticipant<ISiloLifecycle>)participant;
        });
    }
}
