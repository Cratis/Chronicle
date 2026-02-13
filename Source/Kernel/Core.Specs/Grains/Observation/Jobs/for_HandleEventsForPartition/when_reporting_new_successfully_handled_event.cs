// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_HandleEventsForPartition;

public class when_reporting_new_successfully_handled_event : given.the_job_step
{
    Task Because() => _jobStep.ReportNewSuccessfullyHandledEvent(4);

#pragma warning disable xUnit1004
    [Fact(Skip = "Orleans TestKit does not implement GrainFactory.GetGrain(GrainId)")] void should_not_fail() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(4ul);
#pragma warning restore xUnit1004
}
