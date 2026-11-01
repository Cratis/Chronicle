// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Jobs.Placement.for_JobStepPlacementProperties.when_populating;

public class with_enforcement_off_for_a_job_step : given.a_provider
{
    void Because() => _provider.Populate(typeof(HandleEventsForPartition), default, _properties);

    [Fact] void should_leave_the_manifest_unchanged() => _properties.ShouldBeEmpty();
    [Fact] void should_default_to_no_enforcement() => _options.Clustering.EnforceJobsRole.ShouldBeFalse();
}
