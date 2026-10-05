// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing.with_observer_filters;

public class and_the_event_source_type_filter_excludes_some_events : given.a_performing_job_step_with_observer_filters
{
    protected override ObserverFilters Filters => new([], EventSourceType: "account");

    void Establish() => _eventCursor.Current.Returns([CreateEvent(1UL, eventSourceType: "invoice"), CreateEvent(2UL, eventSourceType: "account")]);

    Task Because() => PerformStep();

    [Fact] void should_deliver_only_the_event_of_the_filtered_event_source_type() => _handledBatches.Single().ShouldEqual([2UL]);
}
