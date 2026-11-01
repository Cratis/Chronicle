// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Integration.Clustering.for_JobsRolePlacement;

public class JobsRoleFixture : ClusteringFixture
{
    public JobStepPlacementSignal Placement { get; } = new();

    protected override bool SplitRoles => false;

    protected override void ConfigureSiloServices(IServiceCollection services, bool isFirstSilo)
    {
        services.Configure<Configuration.ChronicleOptions>(options =>
        {
            options.Clustering.EnforceJobsRole = true;
            options.Clustering.Roles.Jobs = isFirstSilo;
            options.Clustering.Roles.Observers = !isFirstSilo;
            options.Clustering.Roles.EventSequences = !isFirstSilo;
        });
        services.AddSingleton(Placement);
        services.AddSingleton<IIncomingGrainCallFilter, JobStepPlacementFilter>();
    }
}
