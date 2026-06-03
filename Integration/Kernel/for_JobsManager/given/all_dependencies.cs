// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

public class all_dependencies(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
{
    public TheJobStepProcessor JobStepProcessor;
    public IJobStorage JobStorage;
    public IJobStepStorage JobStepStorage;

    void Establish()
    {
        JobStepProcessor = Services.GetRequiredService<TheJobStepProcessor>();
        JobStorage = EventStoreStorage.GetNamespace(EventStore.Namespace.Value).Jobs;
        JobStepStorage = EventStoreStorage.GetNamespace(EventStore.Namespace.Value).JobSteps;
    }
}
