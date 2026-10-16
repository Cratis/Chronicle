// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_derived_source_labels : given.contributions
{
    void Establish()
    {
        Read(100);
        var derived = new SourceId(_label.Value);
        _contributions.Add(new(derived, ReadProtectionContributionKind.ProtectedRead, new(90, derived, EventTypes: [_stockRemoved]), _target, "derived read"));
        _legacy.Add(derived, new(120, derived));
    }

    void Because() => _plan = Merge();

    [Fact] void should_have_one_condition_for_the_wire_label() => _plan.Conditions.Count.ShouldEqual(1);
    [Fact] void should_keep_the_earliest_read_boundary() => _plan.Conditions.Single().After.ShouldEqual((EventSequenceNumber)90UL);
    [Fact] void should_keep_the_source_narrowing() => _plan.Conditions.Single().EventSourceId.ShouldEqual(_label);
    [Fact] void should_cover_both_read_event_types() => _plan.Conditions.Single().EventTypes.ShouldContainOnly(_stockReduced, _stockRemoved);

    record SourceId(string Value) : EventSourceId(Value);
}
