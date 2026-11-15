// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueObserverSubscription;

public class when_copying_with_replaced_event_types : Specification
{
    readonly EventTypeId _previous = new("previous");
    readonly EventTypeId _replacement = new("replacement");
    AppendedEventsQueueObserverSubscription _original;
    AppendedEventsQueueObserverSubscription _copy;
    AppendedEventsQueueObserverSubscription _equivalent;

    void Because()
    {
        EventTypeId[] ids = [_previous];
        var key = new ObserverKey("observer", "store", "namespace", "sequence");
        _original = new(key, ids);
        _ = _original.EventTypeIdSet;
        _copy = _original with { EventTypeIds = [_replacement] };
        _equivalent = new(key, ids);
    }

    [Fact] void should_use_the_replacement_membership() => _copy.EventTypeIdSet.Contains(_replacement).ShouldBeTrue();
    [Fact] void should_drop_the_previous_membership() => _copy.EventTypeIdSet.Contains(_previous).ShouldBeFalse();
    [Fact] void should_leave_the_original_membership_unchanged() => _original.EventTypeIdSet.Contains(_previous).ShouldBeTrue();
    [Fact] void should_preserve_record_equality_after_caching() => _original.ShouldEqual(_equivalent);
    [Fact] void should_reuse_membership_for_the_same_subscription() => ReferenceEquals(_original.EventTypeIdSet, _original.EventTypeIdSet).ShouldBeTrue();
}
