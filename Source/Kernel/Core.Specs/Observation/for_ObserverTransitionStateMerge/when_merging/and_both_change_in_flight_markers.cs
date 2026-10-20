// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_both_change_in_flight_markers : given.a_transition_hook
{
    static readonly Key _hookRemoved = "hook-removed";
    static readonly Key _handlingRemoved = "handling-removed";
    static readonly Key _hookAdded = "hook-added";
    static readonly Key _handlingAdded = "handling-added";

    void Establish()
    {
        _handed = _handed with { InFlightPartitions = new HashSet<Key> { _hookRemoved, _handlingRemoved } };
        _recorded = _handed with { InFlightPartitions = new HashSet<Key> { _hookRemoved, _handlingAdded } };
        _returned = _handed with { InFlightPartitions = new HashSet<Key> { _handlingRemoved, _hookAdded } };
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_keep_only_the_added_markers() => _result.InFlightPartitions.ShouldContainOnly([_hookAdded, _handlingAdded]);
    [Fact] void should_not_mutate_the_handed_markers() => _handed.InFlightPartitions.ShouldContainOnly([_hookRemoved, _handlingRemoved]);
    [Fact] void should_not_mutate_the_recorded_markers() => _recorded.InFlightPartitions.ShouldContainOnly([_hookRemoved, _handlingAdded]);
    [Fact] void should_not_mutate_the_returned_markers() => _returned.InFlightPartitions.ShouldContainOnly([_handlingRemoved, _hookAdded]);
}
