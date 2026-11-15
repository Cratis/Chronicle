// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_the_occurrence_is_namespaced : given.an_attempt_with_a_delivery_hook
{
    Task Because() => Run(new(false, "tenant-x"));

    [Fact] void should_enter_the_delivery_scope_for_the_namespace_before_activating_the_handler() => _log.Take(2).ShouldEqual(["enter:tenant-x", "activated:tenant-x"]);
    [Fact] void should_leave_the_delivery_scope_last() => _log[^1].ShouldEqual("leave");
    [Fact] void should_not_leave_a_delivery_context_behind() => Observation.ObserverDeliveryContext.Current.ShouldBeNull();
}
