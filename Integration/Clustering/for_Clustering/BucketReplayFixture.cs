// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Globalization;
using Cratis.Chronicle.Storage;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class BucketReplayFixture : ClusteringFixture
{
    public int BucketCount { get; set; } = 8;
    public ReplayCursorCheckpoint Checkpoint { get; } = new();

    protected override void ConfigureFixtureServices(IServiceCollection services)
    {
        ConfigureBuckets(services);
        var descriptor = services.Last(service => service.ServiceType == typeof(IStorage));
        services.AddSingleton<IStorage>(provider => BucketReplayStorage.Decorate(ResolveStorage(descriptor, provider), Checkpoint));
    }

    void ConfigureBuckets(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jobs:ProjectionRebuildBuckets"] = BucketCount.ToString(CultureInfo.InvariantCulture),
            ["ReadModels:GuardSinkWritesOnWatermark"] = "true"
        }).Build();
        services.Configure<Configuration.ChronicleOptions>(configuration);
        services.PostConfigure<Configuration.ChronicleOptions>(options => options.Clustering.Roles.Observers = true);
    }

    static IStorage ResolveStorage(ServiceDescriptor descriptor, IServiceProvider provider) =>
        descriptor.ImplementationInstance as IStorage ?? descriptor.ImplementationFactory?.Invoke(provider) as IStorage ??
        (IStorage)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
}
