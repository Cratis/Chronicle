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

    [Fact] void should_keep_the_earliest_global_boundary() => _plan.Scopes[_label].SequenceNumber.ShouldEqual((EventSequenceNumber)80u);
    [Fact] void should_keep_the_common_stream_type() => _plan.Scopes[_label].EventStreamType!.Value.ShouldEqual("Transactions");
    [Fact] void should_drop_the_different_stream_ids() => _plan.Scopes[_label].EventStreamId.ShouldBeNull();
    [Fact] void should_union_the_handled_type_ids() => _plan.Scopes[_label].EventTypes!.Select(_ => _.Id).ShouldContainOnly(_stockReduced.Id, _stockRemoved.Id);
}
