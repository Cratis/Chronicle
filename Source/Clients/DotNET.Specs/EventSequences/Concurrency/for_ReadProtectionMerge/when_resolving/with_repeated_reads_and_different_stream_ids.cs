// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_repeated_reads_and_different_stream_ids : given.contributions
{
    void Establish()
    {
        Add(ReadProtectionContributionKind.ProtectedRead, new(100, _label, "Transactions", "January", EventTypes: [_stockReduced]));
        Add(ReadProtectionContributionKind.ProtectedRead, new(80, _label, "Transactions", "February", EventTypes: [_stockRemoved]));
        Add(ReadProtectionContributionKind.ProtectedRead, new(120, _label, "Transactions", "January", EventTypes: [_stockReduced]));
    }

    void Because() => _plan = Merge();

    [Fact] void should_keep_each_predicate() => _plan.Scopes.Count.ShouldEqual(2);
    [Fact] void should_keep_the_earliest_january_boundary() => _plan.Scopes[0].Value.SequenceNumber.ShouldEqual((EventSequenceNumber)100u);
    [Fact] void should_keep_the_february_boundary() => _plan.Scopes[1].Value.SequenceNumber.ShouldEqual((EventSequenceNumber)80u);
    [Fact] void should_keep_the_stream_ids() => _plan.Scopes.Select(_ => _.Value.EventStreamId!.Value).ShouldContainOnly("January", "February");
    [Fact] void should_not_broaden_the_january_type_set() => _plan.Scopes[0].Value.EventTypes!.Select(_ => _.Id).ShouldContainOnly(_stockReduced.Id);
}
