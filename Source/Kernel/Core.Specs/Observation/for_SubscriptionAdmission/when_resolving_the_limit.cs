// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_SubscriptionAdmission;

public class when_resolving_the_limit : Specification
{
    [Fact] void should_use_a_configured_limit() => SubscriptionAdmission.ResolveLimit(3, 32).ShouldEqual(3);
    [Fact] void should_use_two_per_processor_when_not_configured() => SubscriptionAdmission.ResolveLimit(0, 6).ShouldEqual(12);
    [Fact] void should_treat_a_negative_limit_as_not_configured() => SubscriptionAdmission.ResolveLimit(-1, 4).ShouldEqual(8);
    [Fact] void should_not_go_below_the_minimum_on_a_small_machine() => SubscriptionAdmission.ResolveLimit(0, 1).ShouldEqual(SubscriptionAdmission.MinimumAutomaticLimit);
    [Fact] void should_not_go_above_the_maximum_on_a_large_machine() => SubscriptionAdmission.ResolveLimit(0, 64).ShouldEqual(SubscriptionAdmission.MaximumAutomaticLimit);
    [Fact] void should_admit_the_resolved_limit() => new SubscriptionAdmission(5, 1).Limit.ShouldEqual(5);
}
