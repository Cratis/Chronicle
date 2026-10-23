// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public class ReplayPlacementFixture : ClusteringFixture
{
    public ReplayPlacementSignal Placement { get; } = new();

    protected override void ConfigureFixtureServices(IServiceCollection services)
    {
        services.AddOptions<Configuration.ChronicleOptions>().Configure<ILocalSiloDetails>((options, local) =>
        {
            // A = Silo_1 owns live observers and event sequences; B = Silo_0 owns jobs only.
            var live = local.Name == "Silo_1";
            options.Clustering.Roles.Observers = live;
            options.Clustering.Roles.EventSequences = live;
            options.Clustering.Roles.Jobs = !live;
        });
        services.AddSingleton(Placement);
        services.AddSingleton<ReplayPlacementFilter>();
        services.AddSingleton<IIncomingGrainCallFilter>(provider => provider.GetRequiredService<ReplayPlacementFilter>());
        services.AddSingleton<IOutgoingGrainCallFilter>(provider => provider.GetRequiredService<ReplayPlacementFilter>());
    }
}
