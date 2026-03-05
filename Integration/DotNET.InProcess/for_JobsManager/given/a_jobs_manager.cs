// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.InProcess.Integration.for_JobsManager.given;

public class a_jobs_manager(ChronicleInProcessFixture chronicleInProcessFixture) : all_dependencies(chronicleInProcessFixture)
{
    public IJobsManager JobsManager;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new TheJobStepProcessor());

    void Establish()
    {
        JobsManager = GrainFactory.GetJobsManager(EventStore.Name.Value, EventStore.Namespace.Value);
    }
}
