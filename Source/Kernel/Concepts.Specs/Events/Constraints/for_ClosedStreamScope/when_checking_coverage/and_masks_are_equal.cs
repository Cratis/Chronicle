// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStreamScope.when_checking_coverage;

public class and_masks_are_equal : Specification
{
    bool _covers;

    void Because() => _covers = new ClosedStreamScope(EventStreamType: "transactions", EventStreamId: "month").Covers(new(EventStreamType: "transactions", EventStreamId: "month"));

    [Fact] void should_cover_the_exact_scope() => _covers.ShouldBeTrue();
}
