// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_adjusting_totals_without_counts : given.running_totals
{
    ObserverState _subtracted;

    void Because()
    {
        _result = Increment(_state, new Dictionary<EventTypeId, EventCount>());
        _subtracted = Subtract(_state, new Dictionary<EventTypeId, EventCount>());
    }

    [Fact] void should_reuse_the_state_when_incrementing_nothing() => ReferenceEquals(_state, _result).ShouldBeTrue();
    [Fact] void should_reuse_the_state_when_subtracting_nothing() => ReferenceEquals(_state, _subtracted).ShouldBeTrue();
}
