// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDurableSubscriptionFilters;

public class when_restoring_filters : Specification
{
    ObserverFilters _restored;
    ObserverFilters _restoredWithoutTypes;
    ObserverDurableSubscriptionFilters _none;

    void Because()
    {
        _restored = ObserverDurableSubscriptionFilters.From(new ObserverFilters(["a", "b"], new EventSourceType("source"), new EventStreamType("stream")))!.ToObserverFilters();
        _restoredWithoutTypes = ObserverDurableSubscriptionFilters.From(new ObserverFilters(["a"]))!.ToObserverFilters();
        _none = ObserverDurableSubscriptionFilters.From(null);
    }

    [Fact] void should_restore_the_tags() => string.Join(',', _restored.Tags).ShouldEqual("a,b");
    [Fact] void should_restore_the_event_source_type() => _restored.EventSourceType.ShouldEqual(new EventSourceType("source"));
    [Fact] void should_restore_the_event_stream_type() => _restored.EventStreamType.ShouldEqual(new EventStreamType("stream"));
    [Fact] void should_leave_an_absent_event_source_type_absent() => _restoredWithoutTypes.EventSourceType.ShouldBeNull();
    [Fact] void should_leave_an_absent_event_stream_type_absent() => _restoredWithoutTypes.EventStreamType.ShouldBeNull();
    [Fact] void should_store_nothing_when_the_subscription_has_no_filters() => _none.ShouldBeNull();
}
