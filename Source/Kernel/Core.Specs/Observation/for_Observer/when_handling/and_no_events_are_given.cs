// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_no_events_are_given : given.a_traced_observer
{
    Task Because() => _observer.Handle("partition", []);

    [Fact] void should_not_trace_the_delivery() => _activities.ShouldBeEmpty();
}
