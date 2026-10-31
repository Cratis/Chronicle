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

    [Fact] void should_keep_each_predicate_for_the_wire_label() => _plan.Conditions.Count.ShouldEqual(2);
    [Fact] void should_keep_each_read_boundary() => _plan.Conditions.Select(_ => _.After).ShouldContainOnly((EventSequenceNumber)100UL, (EventSequenceNumber)90UL);
    [Fact] void should_keep_the_source_narrowing() => _plan.Conditions.Select(_ => _.EventSourceId).Distinct().ShouldContainOnly(_label);
    [Fact] void should_keep_both_type_predicates() => _plan.Conditions.SelectMany(_ => _.EventTypes).ShouldContainOnly(_stockReduced, _stockRemoved);

    record SourceId(string Value) : EventSourceId(Value);
}
