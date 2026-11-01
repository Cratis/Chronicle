// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Jobs.Placement.for_JobStepPlacementProperties.when_populating;

public class with_enforcement_on_for_another_grain : given.a_provider
{
    void Establish() => _options.Clustering.EnforceJobsRole = true;

    void Because() => _provider.Populate(typeof(ReplayObserver), default, _properties);

    [Fact] void should_leave_controller_placement_unchanged() => _properties.ShouldBeEmpty();
}
