// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Jobs;
namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

public class all_dependencies(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
{
    public IJobsManager JobsManager;
    public TheJobStepProcessor TheJobStepProcessor;
    public IJobStorage JobStorage;
    public IJobStepStorage JobStepStorage;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new TheJobStepProcessor());

    void Establish()
    {
        JobsManager = GrainFactory.GetJobsManager(EventStore.Name.Value, EventStore.Namespace.Value);
        TheJobStepProcessor = Services.GetRequiredService<TheJobStepProcessor>();
        JobStorage = EventStoreStorage.GetNamespace(EventStore.Namespace.Value).Jobs;
        JobStepStorage = EventStoreStorage.GetNamespace(EventStore.Namespace.Value).JobSteps;
    }
}