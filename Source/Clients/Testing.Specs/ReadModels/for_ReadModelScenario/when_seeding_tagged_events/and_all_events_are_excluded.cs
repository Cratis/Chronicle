// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events;

public class and_all_events_are_excluded : given.a_tagged_scenario
{
    Task Because() => _scenario.Given.ForEventSource(_source).WithTags("red").Events(new UntaggedCredit(100));

    [Fact] void should_leave_state_absent() => _scenario.Instance.ShouldBeNull();
    [Fact] void should_not_report_an_instance_for_the_rejected_source() => _scenario.Instances.ShouldBeEmpty();
}
