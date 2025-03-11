// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Integration.Base;
namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

public class a_jobs_manager(GlobalFixture globalFixture) : all_dependencies(globalFixture)
{
    public IJobsManager JobsManager;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new TheJobStepProcessor());

    void Establish()
    {
        JobsManager = GrainFactory.GetJobsManager(EventStore.Name.Value, EventStore.Namespace.Value);
    }
}
