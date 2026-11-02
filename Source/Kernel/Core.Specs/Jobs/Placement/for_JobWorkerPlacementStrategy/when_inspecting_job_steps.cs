// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementStrategy;

public class when_inspecting_job_steps : Specification
{
    Type[] _jobSteps;

    void Because() => _jobSteps = typeof(HandleEventsForPartition).Assembly.GetTypes().Where(IsJobStep).ToArray();

    [Fact] void should_inspect_existing_job_steps() => _jobSteps.ShouldNotBeEmpty();
    [Fact] void should_not_apply_job_worker_placement_yet() => _jobSteps.Where(type => type.IsDefined(typeof(JobWorkerPlacementAttribute), inherit: true)).ShouldBeEmpty();

    static bool IsJobStep(Type type) => type.BaseType is { } baseType &&
        ((baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(JobStep<,,>)) || IsJobStep(baseType));
}
