// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs.Placement;
using Orleans.Metadata;

namespace Cratis.Chronicle.Jobs.Placement.for_JobStepPlacementProperties.when_populating;

public class with_enforcement_on_for_a_job_step : given.a_provider
{
    void Establish() => _options.Clustering.EnforceJobsRole = true;

    void Because() => _provider.Populate(typeof(HandleEventsForPartition), default, _properties);

    [Fact] void should_assign_job_step_placement() => _properties[WellKnownGrainTypeProperties.PlacementStrategy].ShouldEqual(nameof(JobStepsPlacementStrategy));
}
